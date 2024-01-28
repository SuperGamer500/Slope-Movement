using System;
using Unity.VisualScripting;
using UnityEngine;



[RequireComponent(typeof(Rigidbody2D))]
public class MobMovement : MonoBehaviour
{
    [SerializeField] float maxSpeed = 10f;
    public float speedX = 0;
    public float speedY = 0;

    [SerializeField] float acceleration = 0.5f;
    [SerializeField] float deccerleration = 0.5f;
    [SerializeField] float skidSpeed = 0.35f;
    [SerializeField] float velPower = 0.5f;

    [SerializeField] groundState currentGroundState;
    [SerializeField] GameObject groundCheckObj;
    [SerializeField] GameObject slopeCheckObj;


    private float slopeAngle;
    [SerializeField] float maxSlopeAngle = 60;


    Collider2D currentCol;


    (Vector2 rightM, Vector2 leftM, Vector2 middleM, Vector2 rightT, Vector2 leftT, Vector2 middleT, Vector2 rightB, Vector2 leftB, Vector2 middleB) sidesTuple;



    [SerializeField] LayerMask groundLayers;
    [SerializeField] float jumpPower = 10;
    [SerializeField] float gravity = 15.5f;
    [SerializeField] float terminalVelocity = 100f;
    [SerializeField] float airResitance = 0.5f;
    [SerializeField] float normalFallMultiplier = 2f;
    [SerializeField] float fastFallMultiplier = 3f;
    [SerializeField] float jumpBuffer = 0.2f;
    private float currentJBTime = 0;

    private Vector2 groundNormal;
    private Vector2 perpendicularNormal;
    [SerializeField] Vector2 moveVector;
    public Vector2 jumpVector;


    bool? onslope = false;
    bool? lastSlopeBool = false;

    private Rigidbody2D plrRigid;

    [SerializeField] bool rotWithMove = true;

    Vector2 calculatedVel = new Vector2(0, 0);



    public Vector2 requestedDirection;
    float lockMovement = 0;
    [SerializeField] moveStyle moveType;

    private float slideGracePeriod = 0;
    // Start is called before the first frame update
    void Start()
    {

        if (currentCol == null)
        {
            currentCol = GetComponent<Collider2D>();
        }

        if (moveType == moveStyle.normal)
        {
            slopeCheckObj.GetComponent<SpriteRenderer>().enabled = false;
            groundCheckObj.GetComponent<SpriteRenderer>().enabled = false;
        }

        plrRigid = GetComponent<Rigidbody2D>();
    }

    void moveFunction()
    {

        Vector2 input = Vector2.zero;

        //if lockmovement timer is not on
        if (lockMovement > 0)
        {
            //set input to requested direction
            input = requestedDirection;
        }
        else
        {
            //if movetype is normal
            if (moveType == moveStyle.normal)
            {
                //if the current slope angle is less than the max angle
                if (Mathf.Abs(slopeAngle) < maxSlopeAngle)
                {
                    //accecpt requested direction
                    input = new Vector2(requestedDirection.x, 0);
                }
                else if (slideGracePeriod <= 0)
                {
                    //change input to be of the direction of the slope
                    jumpVector.y = 0;

                    input = new Vector2(-MathF.Sign(groundNormal.x) * Mathf.Sign(slopeAngle), 0);
                }

            }
            //else if airborne
            else
            {
                //dont change requested direction
                input = new Vector2(requestedDirection.x, requestedDirection.y);
            }

        }

        //if rotWithMove is on the object will rotated based on x movement
        if (rotWithMove)
        {
            if (input.x > 0)
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
            }
            else if (input.x < 0)
            {
                transform.eulerAngles = new Vector3(0, 180, 0);
            }
        }

        //if movetype is normal
        if (moveType == moveStyle.normal)
        {
            //calculate the speed change on this frame
            float speedToUse = 0;
            incrementCalculate(ref speedToUse, ref speedX, input.x);

            // if the obj is falling off of the slope
            // if (onslope != lastSlopeBool && lastSlopeBool == true && onslope == null && jumpVector.y <= 0 && moveVector.y < -0.1f)
            // {
            //     //convert any y speed in the movevector variable to y velocity in the jump vector value
            //     speedX = moveVector.x;
            //     jumpVector = new Vector2(jumpVector.x, moveVector.y);
            // }

            if ((Mathf.Abs(slopeAngle) < 0.1f) && onslope == null && jumpVector.y <= 0 && moveVector.y < -0.1f)
            {
                //convert any y speed in the movevector variable to y velocity in the jump vector value
                speedX = moveVector.x;
                slideGracePeriod = 0.1f;
                jumpVector = new Vector2(jumpVector.x, moveVector.y);
            }
        }
        else
        {
            //calculate speed values for both the x and y values
            float speedToUseX = 0;
            float speedToUseY = 0;
            incrementCalculate(ref speedToUseX, ref speedX, input.x);
            incrementCalculate(ref speedToUseY, ref speedY, input.y);
        }

        //checks corners to prevent deceleration on walls
        #region CornerCheck



        bool leftMCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.leftM, groundLayers, new Vector2(0.1f, 0.1f));

        bool leftTCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.leftT, groundLayers, new Vector2(0.1f, 0.1f));

        bool rightMCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.rightM, groundLayers, new Vector2(0.1f, 0.1f));

        bool rightTCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.rightT, groundLayers, new Vector2(0.1f, 0.1f));

        bool middleTopCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.middleT, groundLayers, new Vector2(0.1f, 0.1f));

        bool middleBottomCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.middleB, groundLayers, new Vector2(0.1f, 0.1f));



        if (leftMCheck == false || (leftTCheck == false && moveType == moveStyle.normal))
        {

            speedX = Mathf.Max(0, speedX);
        }
        else if (rightMCheck == false || (rightTCheck == false && moveType == moveStyle.normal))
        {

            speedX = Mathf.Min(0, speedX);
        }

        if (middleTopCheck == false)
        {
            speedY = Mathf.Min(0, speedY);
        }
        else if (middleBottomCheck == false)
        {

            speedY = Mathf.Max(0, speedY);
        }
        #endregion

        //if movetype is normal
        if (moveType == moveStyle.normal)
        {
            //Debug.Log(slopeAngle);
            //movector is equal to the x speed times the ground normal
            moveVector = speedX * groundNormal;
        }
        //if airborne
        else
        {
            //normalize the speed x and y values
            Vector2 speedNormalized = new Vector2(speedX, speedY).normalized;

            //multiply normalized vector by the speed x and y values
            moveVector = new Vector2(speedNormalized.x * Mathf.Abs(speedX), speedNormalized.y * Mathf.Abs(speedY));
        }
    }

    void incrementCalculate(ref float speedToUse, ref float currentSpeed, float inputValue)
    {
        speedToUse = 0;

        if (inputValue >= 0 || inputValue == 0)
        {
            //if input is positive or 0 make the max speed positive
            speedToUse = maxSpeed;
        }
        else
        {
            //if input is negative make the max speed negative
            speedToUse = -maxSpeed;
        }

        float difference = Mathf.Abs(speedToUse - currentSpeed);

        string accelOrDecel = "accel";

        //if input is moving
        if (inputValue != 0)
        {

            accelOrDecel = "accel";
        }
        // if input is zero
        else if (inputValue == 0)
        {
            accelOrDecel = "decel";
        }

        float increment = 0;
        if (accelOrDecel == "accel")
        {
            //if speed to use is  not the same sign as current speed
            if (Mathf.Sign(speedToUse) != Mathf.Sign(currentSpeed))
            {
                // accelerate using the skid speed until they are the same sign
                increment = Mathf.Pow(Mathf.Abs((difference) / maxSpeed), velPower) * skidSpeed * Mathf.Sign(speedToUse);
            }
            else
            {
                // accelerate using the accelerate speed
                increment = Mathf.Pow(Mathf.Abs((difference) / maxSpeed), velPower) * acceleration * Mathf.Sign(speedToUse);
            }

            //add the increment to the current speed
            //speed is clamp to the maxspeed to prevent going over
            currentSpeed = Mathf.Clamp(currentSpeed + increment, -maxSpeed, maxSpeed);
        }
        else if (accelOrDecel == "decel")
        {
            //if current speed is positive
            if (currentSpeed > 0)
            {
                //slow down in the negative direction
                increment = Mathf.Pow(Mathf.Abs((currentSpeed - 0) / maxSpeed), velPower) * -deccerleration;
            }
            else
            {
                //slow down in the positive direction
                increment = Mathf.Pow(Mathf.Abs((currentSpeed - 0) / maxSpeed), velPower) * deccerleration;
            }

            //if the distance between the speed an the increment is greater than the increment value
            if (Mathf.Abs(currentSpeed) - Mathf.Abs(increment) > 0)
            {
                // clamp speed to max speeds and then add it to the increment
                currentSpeed = Mathf.Clamp(currentSpeed + increment, -maxSpeed, maxSpeed);
            }
            else
            {
                //set currentspeed to 0
                currentSpeed = 0;
            }
        }

    }
    void gravFunction()
    {
        //if not fully on the ground yet
        if (onGround("grav") == false)
        {
            float yValue = 0;
            float xValue = 0;
            // if the obj lets go of the jump button early
            if ((currentGroundState == groundState.jumping) && Input.GetKey(KeyCode.Space) == false)
            {
                //set state to fastfalling
                currentGroundState = groundState.fastFall;
            }
            //if jumpvector has not reached terminal velocity yet
            if (jumpVector.y > -terminalVelocity)
            {
                //reduce speed by fall values

                if (currentGroundState == groundState.fastFall)
                {
                    yValue = Mathf.Max(-terminalVelocity, jumpVector.y - (gravity * Time.deltaTime * fastFallMultiplier));
                }
                else if (currentGroundState == groundState.falling)
                {
                    yValue = Mathf.Max(-terminalVelocity, jumpVector.y - (gravity * Time.deltaTime * normalFallMultiplier));
                }
                else
                {
                    yValue = Mathf.Max(-terminalVelocity, jumpVector.y - (gravity * Time.deltaTime));
                }
            }

            //if the jumpvector has and x value
            if (jumpVector.x != 0)
            {
                //if the obj is moving in the opositite direction of the jump vector x
                if (moveVector.x != 0 && Mathf.Sign(moveVector.x) != Mathf.Sign(jumpVector.x))
                {
                    //remove x value
                    xValue = 0;
                }
                //else slowly remove x by the airresitance depending on its sign
                else if (Mathf.Sign(jumpVector.x) == 1)
                {
                    xValue = Mathf.Max(jumpVector.x - airResitance * Time.deltaTime, 0);
                }
                else if (Mathf.Sign(jumpVector.x) == -1)
                {
                    xValue = Mathf.Min(jumpVector.x + airResitance * Time.deltaTime, 0);
                }

            }

            jumpVector = new Vector2(xValue, yValue);

            //if not on ground and not fastfalling
            if (jumpVector.y < 0 && onGround() == false && currentGroundState != groundState.fastFall)
            {
                //set state to falling
                currentGroundState = groundState.falling;
            }
        }
        else
        {
            //if on ground and the player is not jumping
            if (onGround("grav") == true && currentGroundState != groundState.jumping && slideGracePeriod <= 0)
            {
                jumpVector = new Vector2(0, 0);
            }
        }

        //if on ground and not jumping
        bool falling = (currentGroundState == groundState.falling || currentGroundState == groundState.fastFall);
        if ((falling || currentGroundState == groundState.jumping && jumpVector.y < 0.1f) && onGround())
        {
            currentGroundState = groundState.grounded;
        }
        //removes jumpvector y if the object collides with wall
        #region CornerCheck



        bool middleTopCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.middleT, groundLayers, new Vector2(0.1f, 0.1f));

        bool leftTopCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.leftT, groundLayers, new Vector2(0.1f, 0.1f));

        bool rightTopCheck = SlopeStaticClass.canGo(gameObject, sidesTuple.rightT, groundLayers, new Vector2(0.1f, 0.1f));



        if (middleTopCheck == false && jumpVector.y > 0.1f)
        {
            jumpVector.y = Mathf.Clamp(jumpVector.y, int.MinValue, 0);
        }
        #endregion

    }
    public void jumpRequest()
    {

        //if object is jumping in the direction of the slope
        if (MathF.Sign(slopeAngle) == Mathf.Sign(moveVector.x) && slopeAngle == 0)
        {
            slideGracePeriod = 0.05f;
            //give extra jump height
            jumpVector = new Vector2(0, 1) * jumpPower * 1.25f;
            currentGroundState = groundState.jumping;
        }
        else
        {
            slideGracePeriod = 0.05f;
            //jump based on the slope 
            jumpVector = (perpendicularNormal * 0.5f + new Vector2(0, 1) * 0.5f) * jumpPower;
            currentGroundState = groundState.jumping;

            //if sliding
            if (Mathf.Abs(slopeAngle) > maxSlopeAngle)
            {
                //if the player is moveing towards the slope
                if (Mathf.Sign(moveVector.x) != Mathf.Sign(jumpVector.x))
                {
                    //reset x
                    speedX = 0;
                }
                //lock movement for a little bit
                lockMovement = 0.15f;
            }
        }

        currentGroundState = groundState.jumping;
    }
    public bool onGround(string purpose = "state")
    {
        purpose.ToLower();
        //use for detecting ground states
        if (purpose == "state")
        {
            int numberOfHits = 0;
            foreach (Collider2D col in Physics2D.OverlapBoxAll(groundCheckObj.transform.position, groundCheckObj.transform.lossyScale, groundCheckObj.transform.eulerAngles.z, groundLayers))
            {
                if (col.gameObject != currentCol.gameObject)
                {
                    numberOfHits++;
                }

            }
            return numberOfHits > 0;
        }
        //used for gravity calculations
        else if (purpose == "grav")
        {
            Ray2D leftRay = new Ray2D(sidesTuple.leftB, Vector2.down);

            Ray2D rightRay = new Ray2D(sidesTuple.rightB, Vector2.down);

            Ray2D middleRay = new Ray2D(sidesTuple.middleB, Vector2.down);

            RaycastHit2D castLeft = Physics2D.Raycast(leftRay.origin, leftRay.direction, 0.1f, groundLayers);

            RaycastHit2D castRight = Physics2D.Raycast(rightRay.origin, rightRay.direction, 0.1f, groundLayers);


            RaycastHit2D castMiddle = Physics2D.Raycast(middleRay.origin, middleRay.direction, 0.1f, groundLayers);

            int numberOfHits = 0;
            foreach (Collider2D col in Physics2D.OverlapBoxAll(sidesTuple.leftB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundLayers))
            {
                if (col.gameObject != currentCol.gameObject)
                {
                    numberOfHits++;
                }

            }
            foreach (Collider2D col in Physics2D.OverlapBoxAll(sidesTuple.rightB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundLayers))
            {
                if (col.gameObject != currentCol.gameObject)
                {
                    numberOfHits++;
                }

            }
            foreach (Collider2D col in Physics2D.OverlapBoxAll(sidesTuple.middleB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundLayers))
            {
                if (col.gameObject != currentCol.gameObject)
                {
                    numberOfHits++;
                }

            }
            return numberOfHits > 0;


        }
        else
        {
            return false;
        }

    }
    private void Update()
    {
        //get sides of obj
        #region getSides
        sidesTuple.leftM = currentCol ? (Vector2)gameObject.transform.position - new Vector2(currentCol.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.rightM = currentCol ? (Vector2)gameObject.transform.position + new Vector2(currentCol.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.middleM = currentCol ? (Vector2)gameObject.transform.position : Vector3.zero;

        sidesTuple.leftT = currentCol ? sidesTuple.leftM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.rightT = currentCol ? sidesTuple.rightM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.middleT = currentCol ? sidesTuple.middleM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;

        sidesTuple.leftB = currentCol ? sidesTuple.leftM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.rightB = currentCol ? sidesTuple.rightM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.middleB = currentCol ? sidesTuple.middleM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;


        #endregion

        //reduce lock movement timer
        lockMovement = Mathf.Max(lockMovement - Time.deltaTime, 0);

        slideGracePeriod = Mathf.Max(slideGracePeriod - Time.deltaTime, 0);

        //can only be used by the player
        #region playerSection
        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            rotWithMove = !Input.GetKey(KeyCode.LeftShift);

            requestedDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            if (moveType == moveStyle.normal)
            {
                //if jump is pressed  or the jump buffer is on while on the ground
                if (onGround() && (Input.GetKeyDown(KeyCode.Space) || currentJBTime > 0) && currentGroundState == groundState.grounded)
                {
                    jumpRequest();
                }
                //turn on buffer time if jump isnt satisfied
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    currentJBTime = jumpBuffer;
                }
                currentJBTime = Mathf.Clamp(currentJBTime - Time.deltaTime, 0, jumpBuffer);
            }
        }
        #endregion
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if move style is normal
        if (moveType == moveStyle.normal)
        {
            //hold on to lastSlope value for cases where you are falling off slopes
            lastSlopeBool = onslope;

            //grab the perpendicular, normal, and angle of slope
            (perpendicularNormal, groundNormal, slopeAngle) = SlopeStaticClass.slopify(gameObject, groundLayers, gameObject.GetComponent<Rigidbody2D>().velocity, slopeCheckObj, groundNormal, ref onslope, getPerp: true, colToUse: currentCol, jumpY: jumpVector.y);
        }

        moveFunction();

        //if move style is normal
        if (moveType == moveStyle.normal)
        {
            //consider gravity
            gravFunction();
        }

        //total up velocity
        calculatedVel = moveVector + jumpVector;
        plrRigid.velocity = calculatedVel;
    }
}

[Serializable]
public enum groundState
{
    grounded,
    jumping,
    falling,
    fastFall,
}

[Serializable]
public enum moveStyle
{
    normal,
    airborne,
}