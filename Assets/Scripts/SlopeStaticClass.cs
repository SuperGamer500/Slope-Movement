
using UnityEngine;

static public class SlopeStaticClass
{

    //checks to see if slope obj is collideing with some type of ground
    public static bool slopeFollow(Transform slopCheckTrans, LayerMask groundLayers, (Ray2D right, Ray2D left) rayTuple, bool mustBeAbove = false)
    {
        Vector2 transPos = slopCheckTrans.position;
        Vector2 lossyScale = slopCheckTrans.lossyScale;
        if (mustBeAbove == true)
        {

            Vector2 leftPos = new Vector2(transPos.x - lossyScale.x / 2, transPos.y);
            Vector2 rightPos = new Vector2(transPos.x + lossyScale.x / 2, transPos.y);

            rayTuple.left.origin = leftPos;
            rayTuple.left.direction = Vector2.down;

            rayTuple.right.origin = rightPos;
            rayTuple.right.direction = Vector2.down;


            if (Physics2D.Raycast(rayTuple.left.origin, rayTuple.left.direction, 0.8f, groundLayers) || Physics2D.Raycast(rayTuple.right.origin, rayTuple.right.direction, 0.8f, groundLayers))
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
            return Physics2D.OverlapBox(transPos, lossyScale, slopCheckTrans.eulerAngles.z, groundLayers);
        }

    }

    public static (Vector2, Vector2, float) slopify(Transform objTrans, LayerMask groundLayers, Vector2 currentVelocity, Transform slopeCheckTrans, (Ray2D left, Ray2D middle, Ray2D right) rayTuple, ref float currAngle, Vector2 currentPerp, Vector2 currentNormal, bool grounded, ref bool wasOnSlope, Collider2D[] results, ContactFilter2D filter, Collider2D colToUse = null, float jumpY = 0, float forceDownAmount = 2.5f)
    {
        wasOnSlope = false;
        #region allVariables


        //get collider for side calculations
        colToUse = colToUse == null ? objTrans.gameObject.GetComponent<Collider2D>() : colToUse;
        //create return vector we will use later
        (Vector2 normal, Vector2 perpendicular, float slopeAngle) returnValue = (currentNormal, currentNormal, 0);

        //grab the sides of obj
        (Vector3 left, Vector3 right, Vector3 middle) sidesTuple;

        sidesTuple.left = colToUse ? objTrans.position - new Vector3(colToUse.bounds.size.x / 2, 0, 0) - new Vector3(0, colToUse.bounds.size.y / 2, 0) : Vector3.zero;
        sidesTuple.right = colToUse ? objTrans.position + new Vector3(colToUse.bounds.size.x / 2, 0, 0) - new Vector3(0, colToUse.bounds.size.y / 2, 0) : Vector3.zero;
        sidesTuple.middle = colToUse ? objTrans.position : Vector3.zero;

        //create rayTuble for left middle and right

        rayTuple.left.origin = sidesTuple.left;
        rayTuple.left.direction = Vector2.down;
        rayTuple.right.origin = sidesTuple.right;
        rayTuple.right.direction = Vector2.down;
        rayTuple.middle.origin = sidesTuple.middle;
        rayTuple.middle.direction = Vector2.down;


        //create raycast hit for left middle and right
        (RaycastHit2D left, RaycastHit2D right, RaycastHit2D middle) castTuple;

        //float raySize = 10;
        float raySize = Mathf.Abs(objTrans.position.y - (slopeCheckTrans.position.y + (-slopeCheckTrans.lossyScale.y / 2)));

        castTuple.left = Physics2D.Raycast(rayTuple.left.origin, rayTuple.left.direction, raySize, groundLayers);
        castTuple.right = Physics2D.Raycast(rayTuple.right.origin, rayTuple.right.direction, raySize, groundLayers);
        castTuple.middle = Physics2D.Raycast(rayTuple.middle.origin, rayTuple.middle.direction, raySize, groundLayers);

        //create a raycast hit2d variable that will be used to get normal later
        RaycastHit2D castToUse = castTuple.right;

        //get the Y points of all sides
        (float left, float right, float middle) yPointsTuple = (0, 0, 0);

        //Y points need to be rounded to get correct values
        yPointsTuple.right = castTuple.right.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.right.point.y - objTrans.position.y), 3) : 9999999;
        yPointsTuple.left = castTuple.left.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.left.point.y - objTrans.position.y), 3) : 9999999;
        yPointsTuple.middle = castTuple.middle.collider ? (float)System.Math.Round((double)Mathf.Abs(castTuple.middle.point.y - objTrans.position.y), 3) : 9999999;

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


        #endregion


        (Vector2 right, Vector2 left, Vector2 middle, Vector2 choice) normalTuple = (castTuple.right.normal, castTuple.left.normal, castTuple.middle.normal, Vector2.zero);
        #region slopeCheck
        if (numberClose == 3)
        {
            //if moving right
            if (movingTuple.right == true)
            {
                //check to see if slope if slightly ahead of sprite
                // rayTuple.right = new Ray2D(sidesTuple.right + new Vector3(0.1f, 0, 0), Vector2.down);

                rayTuple.right.origin = sidesTuple.right + new Vector3(0.1f, 0, 0);

                RaycastHit2D testCast = Physics2D.Raycast(rayTuple.right.origin, rayTuple.right.direction, raySize, groundLayers);

                //if the testCast if different from normal cast
                if (testCast.normal != normalTuple.right)
                {
                    //change normal to be the normal slightly in front of obj
                    castTuple.right = testCast;
                    castToUse = castTuple.right;
                    normalTuple.choice = normalTuple.right;
                }
                else
                {
                    //change normal to be the normal middle one
                    castToUse = castTuple.middle;
                    normalTuple.choice = normalTuple.middle;
                }
            }
            //if moving left
            else if (movingTuple.left == true)
            {
                //check to see if slope if slightly ahead of sprite
                //rayTuple.left = new Ray2D(sidesTuple.left - new Vector3(0.1f, 0, 0), Vector2.down);

                rayTuple.left.origin = sidesTuple.left - new Vector3(0.1f, 0, 0);

                RaycastHit2D testCast = Physics2D.Raycast(rayTuple.left.origin, rayTuple.left.direction, raySize, groundLayers);

                //if the testCast if different from normal cast
                if (testCast.normal != normalTuple.left)
                {
                    //change normal to be the normal slightly in front of obj
                    castTuple.left = testCast;
                    castToUse = castTuple.right;
                    normalTuple.choice = normalTuple.right;
                }
                else
                {
                    //change normal to be the normal middle one
                    castToUse = castTuple.middle;
                    normalTuple.choice = normalTuple.middle;
                }
            }
            //if the object is stagnant
            else
            {
                castToUse = castTuple.middle;
                normalTuple.choice = normalTuple.middle;
            }
        }
        else if (numberClose == 2)
        {
            //if the closeest point is the right point and obj is moving to the right
            if (cloesestPoint == yPointsTuple.right && movingTuple.right == true)
            {
                //set cast to the right
                castToUse = castTuple.right;
                normalTuple.choice = normalTuple.right;
            }
            //if the closest point is the left point and obj is moving to the left
            else if (cloesestPoint == yPointsTuple.left && movingTuple.left == true)
            {
                //set cast to the left
                castToUse = castTuple.left;
                normalTuple.choice = normalTuple.left;
            }
            //if the cloest point is the middle point
            else
            {
                //set cast to the middle
                castToUse = castTuple.middle;
                normalTuple.choice = normalTuple.middle;
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

                goTuple.right = CollisionStaticFunctions.canGo(objTrans, objTrans.position + (Vector3)(-Vector2.Perpendicular(castTuple.right.normal) * Mathf.Sign(currentVelocity.x)).normalized * 0.1f, colToUse.bounds.size * 0.9f, ref results, filter);


                if (yPointsTuple.left > yPointsTuple.middle && movingTuple.left == true && goTuple.right == false)
                {
                    //use the left instead of right
                    castToUse = castTuple.left;
                    normalTuple.choice = normalTuple.left;
                }
                else
                {
                    //if the obj can realistically go in that direction without going into colision
                    if (goTuple.right)
                    {
                        //use right cast
                        castToUse = castTuple.right;
                        normalTuple.choice = normalTuple.right;
                    }
                    else
                    {
                        //use middle cast instead
                        castToUse = castTuple.middle;
                        normalTuple.choice = normalTuple.middle;
                    }
                }
            }
            else if (cloesestPoint == yPointsTuple.left)
            {


                goTuple.left = CollisionStaticFunctions.canGo(objTrans, objTrans.position + (Vector3)(-Vector2.Perpendicular(castTuple.left.normal) * Mathf.Sign(currentVelocity.x)).normalized * 0.1f, colToUse.bounds.size * 0.9f, ref results, filter);
                if (yPointsTuple.right > yPointsTuple.middle && movingTuple.right == true && goTuple.left == false)
                {
                    //cast right instead of left
                    castToUse = castTuple.right;
                    normalTuple.choice = normalTuple.right;
                }
                else
                {
                    //if object can move in the go in the direction of the highest Y point whilst not hitting collsion
                    if (goTuple.left)
                    {
                        //cast left
                        castToUse = castTuple.left;
                        normalTuple.choice = normalTuple.left;
                    }
                    else
                    {
                        //cast middle instead
                        castToUse = castTuple.middle;
                        normalTuple.choice = normalTuple.middle;
                    }
                }
            }
            //if middle is the highest point of all
            else
            {
                //cast to middle
                castToUse = castTuple.middle;
                normalTuple.choice = normalTuple.middle;
            }
        }
        #endregion

        #region getSlope
        //if slope is found and the slope obj is overlapping with the ground
        if (castToUse.collider && slopeFollow(slopeCheckTrans, groundLayers, (rayTuple.right, rayTuple.left)))
        {


            if (currentPerp != normalTuple.choice)
            {
                currAngle = Mathf.Atan2(normalTuple.choice.y, normalTuple.choice.x) * Mathf.Rad2Deg - 90;
                //if slope angle is near zero
                if (Mathf.Abs(currAngle) > 0.1f)
                {
                    //if the obj is not near the ground
                    if (jumpY < 0.1f && grounded == false && forceDownAmount > 0)
                    {
                        // attempt to force the obj closer to the ground if the obj is not jumping

                        Vector3 threeNormal = (Vector3)(normalTuple.choice);
                        Vector3 nextPos = objTrans.position - (Vector3)(normalTuple.choice * Time.deltaTime * (forceDownAmount * (currentVelocity.magnitude / 5)));


                        if (CollisionStaticFunctions.canGo(objTrans, nextPos, colToUse.bounds.size * 0.9f, ref results, filter) && objTrans.position != nextPos)
                        {
                            objTrans.position = nextPos;
                        }
                        else
                        {
                            float speedAmount = forceDownAmount * (currentVelocity.magnitude / 5);
                            while (speedAmount > 0)
                            {
                                nextPos = objTrans.position - (threeNormal * Time.deltaTime * speedAmount);

                                if (CollisionStaticFunctions.canGo(objTrans, nextPos, colToUse.bounds.size, ref results, filter))
                                {
                                    objTrans.position = nextPos;
                                    break;
                                }
                                speedAmount -= 0.1f;
                            }
                        }
                    }
                }
            }

            //get return values
            returnValue.perpendicular = -Vector2.Perpendicular(normalTuple.choice);
            returnValue.normal = normalTuple.choice;
            returnValue.slopeAngle = currAngle;
        }
        else
        {
            //turn slope off and get return values
            returnValue.perpendicular = new Vector2(1, 0);
            returnValue.normal = new Vector2(0, 1);
            returnValue.slopeAngle = 0;
        }
        #endregion

        bool currDist = (Vector2.Distance(currentNormal, new Vector2(1, 0)) < 0.1f) == false;
        bool normDist = Vector2.Distance(returnValue.perpendicular, new Vector2(1, 0)) < 0.1f;

        if (currDist && normDist)
        {
            wasOnSlope = true;
        }

        return returnValue;
    }


}
