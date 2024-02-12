using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

static public class SlopeStaticClass
{
    public static bool canGoNoAlloc(GameObject obj, Vector3 nextPosition, LayerMask groundLayers, Vector2 scale, Collider2D[] results, ContactFilter2D filter)
    {
        int numberOfHits = 0;
        Physics2D.OverlapBox(nextPosition, scale, obj.transform.eulerAngles.z, filter, results);
        for (int i = 0; i < results.Length; i++)
        {
            Collider2D col = results[i];
            if (col != null)
            {
                if (obj.gameObject != col.gameObject)
                {
                    results.SetValue(null, i);
                    numberOfHits++;
                }
            }
        }
        return numberOfHits == 0;
    }
    //checks to see if slope obj is collideing with some type of ground
    public static bool slopeFollow(GameObject slopCheckObj, LayerMask groundLayers, bool mustBeAbove = false)
    {
        if (mustBeAbove == true)
        {
            Vector2 leftPos = new Vector2(slopCheckObj.transform.position.x - slopCheckObj.transform.lossyScale.x / 2, slopCheckObj.transform.position.y - slopCheckObj.transform.lossyScale.y / 2);
            Vector2 rightPos = new Vector2(slopCheckObj.transform.position.x + slopCheckObj.transform.lossyScale.x / 2, slopCheckObj.transform.position.y - slopCheckObj.transform.lossyScale.y / 2);

            Ray2D downLeftRay = new Ray2D(leftPos, Vector2.down);
            Ray2D downRightRay = new Ray2D(rightPos, Vector2.down);

            if (Physics2D.Raycast(downLeftRay.origin, downLeftRay.direction, 0.1f, groundLayers) || Physics2D.Raycast(downRightRay.origin, downRightRay.direction, 0.1f, groundLayers))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return Physics2D.OverlapBox(slopCheckObj.transform.position, slopCheckObj.transform.lossyScale, slopCheckObj.transform.eulerAngles.z, groundLayers);
        }

    }

    public static (Vector2, Vector2, float) slopify(GameObject obj, LayerMask groundLayers, Vector2 currentVelocity, GameObject slopeCheckObj, Vector2 currentNormal, bool grounded, ref bool? onSlope, Collider2D[] results, ContactFilter2D filter, bool getPerp = true, Collider2D colToUse = null, float jumpY = 0, float forceDownAmount = 2.5f)
    {
        #region allVariables
        //get collider for side calculations
        colToUse = colToUse == null ? obj.GetComponent<Collider2D>() : colToUse;
        //create return vector we will use later
        (Vector2 normal, Vector2 perpendicular, float slopeAngle) returnValue = (currentNormal, currentNormal, 0);

        //grab the sides of obj
        (Vector3 left, Vector3 right, Vector3 middle) sidesTuple = (Vector3.zero, Vector3.zero, Vector3.zero);
        sidesTuple.left = colToUse ? (Vector2)obj.transform.position - new Vector2(colToUse.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.right = colToUse ? (Vector2)obj.transform.position + new Vector2(colToUse.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.middle = colToUse ? (Vector2)obj.transform.position : Vector3.zero;

        //create rayTuble for left middle and right
        (Ray2D left, Ray2D right, Ray2D middle) rayTuple = (new Ray2D(), new Ray2D(), new Ray2D());

        rayTuple.left = new Ray2D(sidesTuple.left, Vector2.down);
        rayTuple.right = new Ray2D(sidesTuple.right, Vector2.down);
        rayTuple.middle = new Ray2D(sidesTuple.middle, Vector2.down);

        //create raycast hit for left middle and right
        (RaycastHit2D left, RaycastHit2D right, RaycastHit2D middle) castTuple = (new RaycastHit2D(), new RaycastHit2D(), new RaycastHit2D());

        //float raySize = 10;
        float raySize = Mathf.Abs(obj.transform.position.y - (slopeCheckObj.transform.position.y + (-slopeCheckObj.transform.lossyScale.y / 2)));

        castTuple.left = Physics2D.Raycast(rayTuple.left.origin, rayTuple.left.direction, raySize, groundLayers);
        castTuple.right = Physics2D.Raycast(rayTuple.right.origin, rayTuple.right.direction, raySize, groundLayers);
        castTuple.middle = Physics2D.Raycast(rayTuple.middle.origin, rayTuple.middle.direction, raySize, groundLayers);

        //create a raycast hit2d variable that will be used to get normal later
        RaycastHit2D castToUse = castTuple.right;

        //get the Y points of all sides
        (float left, float right, float middle) yPointsTuple = (0, 0, 0);

        //Y points need to be rounded to get correct values
        yPointsTuple.right = castTuple.right.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.right.point.y - obj.transform.position.y), 3) : 9999999;
        yPointsTuple.left = castTuple.left.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.left.point.y - obj.transform.position.y), 3) : 9999999;
        yPointsTuple.middle = castTuple.middle.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.middle.point.y - obj.transform.position.y), 3) : 9999999;

        //get the lowest Y point value meaning it is the closest to the object
        float cloesestPoint = Mathf.Min(yPointsTuple.right, yPointsTuple.left, yPointsTuple.middle);

        // check to see how many points are equal to the closest point
        float numberClose = 0;
        numberClose += yPointsTuple.right == cloesestPoint && yPointsTuple.right != 9999999 ? 1 : 0;
        numberClose += yPointsTuple.left == cloesestPoint && yPointsTuple.left != 9999999 ? 1 : 0;
        numberClose += yPointsTuple.middle == cloesestPoint && yPointsTuple.middle != 9999999 ? 1 : 0;

        //determines if obj is moving in either the left or right position
        (bool? right, bool? left) movingTuple = (null, null);

        if (currentVelocity.x > 0.1f)
        {
            movingTuple.right = true;
        }
        else if (currentVelocity.x < -0.1f)
        {
            movingTuple.right = false;
        }
        else
        {
            movingTuple.right = null;
        }

        if (currentVelocity.x < -0.1f)
        {
            movingTuple.left = true;
        }
        else if (currentVelocity.x > 0.1f)
        {
            movingTuple.left = false;
        }
        else
        {
            movingTuple.left = null;
        }

        //checks to see if the object will move into the wall if they move with the cast normal
        (bool left, bool right) goTuple = (false, false);

        goTuple.right = canGoNoAlloc(obj, obj.transform.position + (Vector3)(-Vector2.Perpendicular(castTuple.right.normal) * Mathf.Sign(currentVelocity.x)).normalized * 0.1f, groundLayers, colToUse.bounds.size * 0.9f, results, filter);

        goTuple.left = canGoNoAlloc(obj, obj.transform.position + (Vector3)(-Vector2.Perpendicular(castTuple.left.normal) * Mathf.Sign(currentVelocity.x)).normalized * 0.1f, groundLayers, colToUse.bounds.size * 0.9f, results, filter);
        #endregion

        #region slopeCheck
        if (numberClose == 3)
        {
            //if moving right
            if (movingTuple.right == true)
            {
                //check to see if slope if slightly ahead of sprite
                rayTuple.right = new Ray2D(sidesTuple.right + new Vector3(0.1f, 0, 0), Vector2.down);

                RaycastHit2D testCast = Physics2D.Raycast(rayTuple.right.origin, rayTuple.right.direction, raySize, groundLayers);

                //if the testCast if different from normal cast
                if (testCast.normal != castTuple.right.normal)
                {
                    //change normal to be the normal slightly in front of obj
                    castTuple.right = testCast;
                    castToUse = castTuple.right;
                }
                else
                {
                    //change normal to be the normal middle one
                    castToUse = castTuple.middle;
                }
            }
            //if moving left
            else if (movingTuple.left == true)
            {
                //check to see if slope if slightly ahead of sprite
                rayTuple.left = new Ray2D(sidesTuple.left - new Vector3(0.1f, 0, 0), Vector2.down);

                RaycastHit2D testCast = Physics2D.Raycast(rayTuple.left.origin, rayTuple.left.direction, raySize, groundLayers);

                //if the testCast if different from normal cast
                if (testCast.normal != castTuple.left.normal)
                {
                    //change normal to be the normal slightly in front of obj
                    castTuple.left = testCast;
                    castToUse = castTuple.right;
                }
                else
                {
                    //change normal to be the normal middle one
                    castToUse = castTuple.middle;
                }
            }
            //if the object is stagnant
            else
            {
                castToUse = castTuple.middle;
            }
        }
        else if (numberClose == 2)
        {
            //if the closeest point is the right point and obj is moving to the right
            if (cloesestPoint == yPointsTuple.right && movingTuple.right == true)
            {
                //set cast to the right
                castToUse = castTuple.right;
            }
            //if the closest point is the left point and obj is moving to the left
            else if (cloesestPoint == yPointsTuple.left && movingTuple.left == true)
            {
                //set cast to the left
                castToUse = castTuple.left;
            }
            //if the cloest point is the middle point
            else
            {
                //set cast to the middle
                castToUse = castTuple.middle;
            }
        }
        else if (numberClose == 1)
        {
            //if the right point is the only cloest point
            if (cloesestPoint == yPointsTuple.right)
            {
                //if left y point is greater than the middle point and less than the right point
                /* if the obj is moving left and if the obj will clip if they continue to move based on right slope
                */
                /*this is for situations where the player should technically
                should be following the direction of the normal but cant because the ground goes back upward after the middle point towards the highest point but not quite to the same magnitude as the highest point
                */
                if (yPointsTuple.left > yPointsTuple.middle && movingTuple.left == true && goTuple.right == false)
                {
                    //use the left instead of right
                    castToUse = castTuple.left;
                }
                else
                {
                    //if the obj can realistically go in that direction without going into colision
                    if (goTuple.right)
                    {
                        //use right cast
                        castToUse = castTuple.right;
                    }
                    else
                    {
                        //use middle cast instead
                        castToUse = castTuple.middle;
                    }
                }
            }
            else if (cloesestPoint == yPointsTuple.left)
            {
                if (yPointsTuple.right > yPointsTuple.middle && movingTuple.right == true && goTuple.left == false)
                {
                    //cast right instead of left
                    castToUse = castTuple.right;
                }
                else
                {
                    //if object can move in the go in the direction of the highest Y point whilst not hitting collsion
                    if (goTuple.left)
                    {
                        //cast left
                        castToUse = castTuple.left;
                    }
                    else
                    {
                        //cast middle instead
                        castToUse = castTuple.middle;
                    }
                }
            }
            //if middle is the highest point of all
            else
            {
                //cast to middle
                castToUse = castTuple.middle;
            }
        }
        #endregion

        #region getSlope
        //if slope is found and the slope obj is overlapping with the ground
        if (castToUse.collider && slopeFollow(slopeCheckObj, groundLayers))
        {
            float angle = Mathf.Atan2(castToUse.normal.y, castToUse.normal.x) * Mathf.Rad2Deg - 90;

            //if slope angle is near zero
            if (Mathf.Abs(angle) < 0.1f)
            {
                //turn slope variable off
                onSlope = false;
            }
            else
            {
                //if the obj is not near the ground
                if (jumpY < 0.1f && grounded == false)
                {
                    // attempt to force the obj closer to the ground if the obj is not jumping
                    Vector3 nextPos = obj.transform.position - (Vector3)(castToUse.normal * (forceDownAmount * (currentVelocity.magnitude / 5)));

                    if (canGoNoAlloc(obj, nextPos, groundLayers, colToUse.bounds.size * 0.9f, results, filter) && obj.transform.position != nextPos)
                    {
                        onSlope = true;
                        obj.transform.position = nextPos;
                    }
                    else
                    {
                        float speedAmount = forceDownAmount * (currentVelocity.magnitude / 5);
                        while (speedAmount > 0)
                        {
                            nextPos = obj.transform.position - (Vector3)(castToUse.normal * speedAmount);

                            if (canGoNoAlloc(obj, nextPos, groundLayers, colToUse.bounds.size, results, filter))
                            {
                                onSlope = true;
                                obj.transform.position = nextPos;
                                break;
                            }
                            speedAmount -= 0.1f;
                        }
                    }
                }
            }
            //get return values
            returnValue.perpendicular = -Vector2.Perpendicular(castToUse.normal);
            returnValue.normal = castToUse.normal;
            returnValue.slopeAngle = angle;
        }
        else
        {
            //turn slope off and get return values
            onSlope = null;
            returnValue.perpendicular = new Vector2(1, 0);
            returnValue.normal = new Vector2(0, 1);
            returnValue.slopeAngle = 0;
        }
        #endregion

        return returnValue;
    }
}
