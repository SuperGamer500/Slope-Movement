using System;
using System.Collections;
using System.Collections.Generic;
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

    public groundState currentGroundState;
    [SerializeField] GameObject groundCheckObj;
    [SerializeField] GameObject slopeCheckObj;
    [SerializeField] bool dynamicSlopeObj = true;


    private float slopeAngle;
    [SerializeField] float maxSlopeAngle = 60;
    [SerializeField] float forceDownAmount = 2.5f;

    Collider2D currentCol;
    (Vector2 rightM, Vector2 leftM, Vector2 middleM, Vector2 rightT, Vector2 leftT, Vector2 middleT, Vector2 rightB, Vector2 leftB, Vector2 middleB) sidesTuple;

    [SerializeField] LayerMask groundLayers = 1 << 0;
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
    public Vector2 moveVector;
    public Vector2 jumpVector;
    public Vector2 otherVector { get; protected set; }
    private bool otherUp;


    [SerializeField] List<otherDictValues> otherDictionary;


    bool? onslope = false;
    private Rigidbody2D rigid;


    public bool rotWithMove = true;

    Vector2 calculatedVel = new Vector2(0, 0);

    public Vector2 requestedDirection;
    public float lockMovement = 0;
    [SerializeField] moveStyle moveType;

    private float slideGracePeriod = 0;

    [SerializeField] private Collider2D[] groundResults = new Collider2D[30];
    [SerializeField] private Collider2D[] slopeResults = new Collider2D[30];
    [SerializeField] private Collider2D[] canGoResults = new Collider2D[30];
    private ContactFilter2D groundFilter;
    private ContactFilter2D slopeFilter;
    private ContactFilter2D canGoFilter;

    // Start is called before the first frame update
    void Start()
    {
        groundFilter.useTriggers = false;
        slopeFilter.useTriggers = false;
        canGoFilter.useTriggers = false;
        if (currentCol == null)
        {
            currentCol = GetComponent<Collider2D>();
        }
        if (moveType == moveStyle.normal)
        {
            slopeCheckObj.GetComponent<SpriteRenderer>().enabled = false;
            groundCheckObj.GetComponent<SpriteRenderer>().enabled = false;
        }
        rigid = GetComponent<Rigidbody2D>();
        if (rigid)
        {
            rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }
    void changeSlopeObj()
    {
        float speedPlus = (Mathf.Abs(speedX) + Mathf.Abs(speedY)) / (10);
        float yCalc = jumpVector.y > 0.1 ? 0.1f : Mathf.Clamp(0.5f * speedPlus, 0.5f, 2);
        if (currentGroundState == groundState.slideing)
        {
            slopeCheckObj.transform.localScale = new Vector3(currentCol.bounds.size.x * 1, yCalc, 1);
        }
        else
        {
            slopeCheckObj.transform.localScale = jumpVector.y > 0.1 ? new Vector3(currentCol.bounds.size.x, yCalc, 1) : new Vector3(currentCol.bounds.size.x, yCalc, 1);
        }

        slopeCheckObj.transform.position = sidesTuple.middleB - new Vector2(0, (slopeCheckObj.transform.lossyScale.y / 2));

        forceDownAmount = Mathf.Abs(moveVector.magnitude) / 20;
    }

    #region otherStuff
    Vector2 combineOtherVector()
    {
        otherUp = false;
        Vector2 returnValue = new Vector2();
        foreach (otherDictValues dictValues in otherDictionary)
        {
            if (dictValues != null)
            {
                returnValue += dictValues.value.x * groundNormal + new Vector2(0, dictValues.value.y);
                if (dictValues.value.y > 0.1f)
                {
                    otherUp = true;
                }
            }
        }
        return returnValue;
    }
    public IEnumerator removeOther(string forceName, Vector2 originalValue, float timeToZero = 0, float power = 1)
    {
        otherDictValues testCheck = null;
        foreach (otherDictValues dictIndex in otherDictionary)
        {
            if (dictIndex.key == forceName)
            {
                testCheck = dictIndex;
            }
        }

        if (testCheck != null)
        {
            testCheck.accelerating = false;
            if (timeToZero > 0)
            {
                Vector2 value = testCheck.value;

                if (originalValue.x != 0 && testCheck.value.x != 0)
                {
                    timeToZero *= testCheck.value.x / originalValue.x;

                }
                else if (originalValue.y != 0 && testCheck.value.y != 0)
                {
                    timeToZero *= testCheck.value.y / originalValue.y;
                }

                float cTime = timeToZero;
                while (cTime > 0)
                {
                    if (testCheck.accelerating == true)
                    {
                        break;
                    }
                    testCheck.value = value * Mathf.Pow(cTime / timeToZero, power);
                    yield return null;
                    cTime -= Time.deltaTime;
                }
                otherDictionary.Remove(testCheck);
            }
            else
            {
                otherDictionary.Remove(testCheck);
            }
        }
    }
    public IEnumerator addOther(string forceName, Vector2 value, float timeToMax = 0, float power = 1, bool removeImdiately = false, float removeTime = 0, bool inverse = false)
    {
        otherDictValues testCheck = new otherDictValues();



        bool alreadyThere = false;
        foreach (otherDictValues dictIndex in otherDictionary)
        {
            if (dictIndex.key == forceName)
            {
                testCheck = dictIndex;
                alreadyThere = true;
                break;
            }
        }

        if (alreadyThere == false || testCheck.accelerating == false)
        {
            testCheck.key = forceName;
            testCheck.value = value;
            testCheck.accelerating = true;
            otherDictionary.Add(testCheck);
            if (timeToMax > 0)
            {
                float cTime = 0;
                while (cTime < timeToMax)
                {
                    if (testCheck.accelerating == false)
                    {
                        break;
                    }
                    if (inverse == false)
                    {
                        testCheck.value = value * Mathf.Pow(cTime / timeToMax, power);
                    }
                    else
                    {
                        testCheck.value = value * Mathf.Pow(1 - (cTime / timeToMax), power);
                    }

                    yield return null;
                    cTime += Time.deltaTime;
                }


            }
            else
            {
                testCheck.value = value;
            }

            if (removeImdiately)
            {
                StartCoroutine(removeOther(forceName, value, removeTime, power));
            }
        }
    }
    #endregion
    #region JumpStuff
    void gravFunction()
    {
        //if not fully on the ground yet
        if (onGround("grav") == false && otherUp == false)
        {
            float yValue = 0;
            float xValue = 0;
            // if the obj lets go of the jump button early
            if (currentGroundState == groundState.jumping && Input.GetKey(KeyCode.Space) == false)
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
                // if ((currentGroundState == groundState.slideing || currentGroundState == groundState.grounded) && onGround() == false)
                // {
                //     Debug.Log("yess");
                //     // Vector3 startingScale = slopeCheckObj.transform.localScale;
                //     // slopeCheckObj.transform.localScale = startingScale * 0.75f;
                //     if (SlopeStaticClass.slopeFollow(slopeCheckObj, groundLayers, true) == false)
                //     {
                //         //if sliding convert yspeed to jump vector
                //         Debug.Log("cancelling");
                //         slideGracePeriod = 0.1f;
                //         jumpVector.y = moveVector.y;
                //         speedX = (groundNormal.x * speedX);
                //         moveVector = new Vector2(speedX, 0);
                //         speedY = 0;
                //     }
                //     else
                //     {
                //         Debug.Log("oh come on");
                //     }
                //     //slopeCheckObj.transform.localScale = startingScale;
                // }
                //set state to falling
                currentGroundState = groundState.falling;
            }
        }
        else
        {
            //if on ground and the player is not jumping
            if (currentGroundState != groundState.jumping && slideGracePeriod <= 0)
            {
                jumpVector = new Vector2(0, 0);
            }
        }

        //if on ground and not jumping
        bool falling = currentGroundState == groundState.falling || currentGroundState == groundState.fastFall || currentGroundState == groundState.jumping;

        if (falling && jumpVector.y < 0.1f && onGround())
        {
            if (Mathf.Abs(slopeAngle) < maxSlopeAngle)
            {
                currentGroundState = groundState.grounded;
            }
            else
            {
                currentGroundState = groundState.slideing;
            }
        }
        //removes jumpvector y if the object collides with wall
        #region CornerCheck
        bool middleTopCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.middleT, groundLayers, new Vector2(0.1f, 0.1f), groundResults, groundFilter);

        bool leftTopCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.leftT, groundLayers, new Vector2(0.1f, 0.1f), groundResults, groundFilter);

        bool rightTopCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.rightT, groundLayers, new Vector2(0.1f, 0.1f), groundResults, groundFilter);

        if (middleTopCheck == false && jumpVector.y > 0.1f)
        {
            jumpVector.y = Mathf.Clamp(jumpVector.y, int.MinValue, 0);
        }
        #endregion
    }
    public void jumpRequest()
    {
        //if object is jumping in the direction of the slope
        if (MathF.Sign(slopeAngle) == Mathf.Sign(moveVector.x) && Mathf.Abs(moveVector.x) != 0 && slopeAngle != 0)
        {


            //give extra jump height
            if (Mathf.Abs(slopeAngle) > maxSlopeAngle)
            {
                jumpVector = (perpendicularNormal * 0.5f + new Vector2(0, 1) * 0.5f) * jumpPower;


                //if the player is moveing towards the slope

                //reset x
                speedX = 0;

                //lock movement for a little bit
                lockMovement = 0.15f;

            }
            else
            {
                jumpVector = new Vector2(0, 1) * jumpPower + new Vector2(0, moveVector.y / 2);
            }
        }
        else
        {

            //jump based on the slope 
            jumpVector = (perpendicularNormal * 0.5f + new Vector2(0, 1) * 0.5f) * jumpPower;
            Debug.Log(jumpVector);


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
        if (groundFilter.layerMask != groundLayers)
        {
            groundFilter.SetLayerMask(groundLayers);
        }
        //use for detecting ground states
        if (purpose == "state")
        {
            int numberOfHits = 0;

            Physics2D.OverlapBox(groundCheckObj.transform.position, groundCheckObj.transform.lossyScale, groundCheckObj.transform.eulerAngles.z, groundFilter, groundResults);
            for (int i = 0; i < groundResults.Length; i++)
            {
                Collider2D col = groundResults[i];
                if (col != null)
                {
                    if (col.gameObject != currentCol.gameObject)
                    {
                        groundResults.SetValue(null, i);
                        numberOfHits++;
                    }
                }
            }
            return numberOfHits > 0;
        }
        //used for gravity calculations
        else if (purpose == "grav")
        {
            grabSides();
            int numberOfHits = 0;

            Physics2D.OverlapBox(sidesTuple.leftB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundFilter, groundResults);

            for (int i = 0; i < groundResults.Length; i++)
            {
                Collider2D col = groundResults[i];
                if (col != null)
                {
                    if (col.gameObject != currentCol.gameObject)
                    {
                        groundResults.SetValue(null, i);
                        numberOfHits++;
                    }
                }
            }

            Physics2D.OverlapBox(sidesTuple.rightB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundFilter, groundResults);

            for (int i = 0; i < groundResults.Length; i++)
            {
                Collider2D col = groundResults[i];
                if (col != null)
                {
                    if (col.gameObject != currentCol.gameObject)
                    {
                        groundResults.SetValue(null, i);
                        numberOfHits++;
                    }
                }
            }

            Physics2D.OverlapBox(sidesTuple.middleB, Vector2.zero, groundCheckObj.transform.eulerAngles.z, groundFilter, groundResults);

            for (int i = 0; i < groundResults.Length; i++)
            {
                Collider2D col = groundResults[i];
                if (col != null)
                {
                    if (col.gameObject != currentCol.gameObject)
                    {
                        groundResults.SetValue(null, i);
                        numberOfHits++;
                    }
                }
            }
            return numberOfHits > 0 && onGround();
        }
        else
        {
            return false;
        }

    }
    #endregion
    #region moveStuff
    void moveFunction()
    {
        Vector2 input = Vector2.zero;
        //if lockmovement timer is on
        if (lockMovement > 0)
        {
            //set input to requested direction
            input = Vector2.zero;
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
        bool leftMCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.leftM, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);

        bool leftTCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.leftT, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);
        bool rightMCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.rightM, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);

        bool rightTCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.rightT, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);

        bool middleTopCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.middleT, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);

        bool middleBottomCheck = SlopeStaticClass.canGoNoAlloc(gameObject, sidesTuple.middleB, groundLayers, new Vector2(0, 0), canGoResults, canGoFilter);

        // if ((leftMCheck == false) && moveType == moveStyle.normal)
        // {
        //     speedX = Mathf.Max(0, speedX);
        // }
        // else if ((rightMCheck == false) && moveType == moveStyle.normal)
        // {
        //     speedX = Mathf.Min(0, speedX);
        // }

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

    #endregion
    void grabSides()
    {
        sidesTuple.leftM = currentCol ? (Vector2)gameObject.transform.position - new Vector2(currentCol.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.rightM = currentCol ? (Vector2)gameObject.transform.position + new Vector2(currentCol.bounds.size.x / 2, 0) : Vector3.zero;
        sidesTuple.middleM = currentCol ? (Vector2)gameObject.transform.position : Vector3.zero;

        sidesTuple.leftT = currentCol ? sidesTuple.leftM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.rightT = currentCol ? sidesTuple.rightM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.middleT = currentCol ? sidesTuple.middleM + new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;

        sidesTuple.leftB = currentCol ? sidesTuple.leftM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.rightB = currentCol ? sidesTuple.rightM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
        sidesTuple.middleB = currentCol ? sidesTuple.middleM - new Vector2(0, currentCol.bounds.size.y / 2) : Vector3.zero;
    }
    private void Update()
    {
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
                bool groundStateCheck = currentGroundState == groundState.grounded || currentGroundState == groundState.slideing;
                //if jump is pressed or the jump buffer is on while on the ground
                if (onGround() && (Input.GetKeyDown(KeyCode.Space) || currentJBTime > 0) && groundStateCheck && jumpVector.y < 0.1f)
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
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.C))
            {
                Time.timeScale = Time.timeScale == 0.2f ? 1 : 0.2f;
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                StartCoroutine(addOther("yay", new Vector2(0, 15), 1, 0.5f, true, 0.5f));
            }
#endif
        }
        #endregion
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        grabSides();

        if (dynamicSlopeObj && slopeCheckObj)
        {
            changeSlopeObj();
        }
        //if move style is normal
        if (moveType == moveStyle.normal)
        {
            gravFunction();
            //grab the perpendicular, normal, and angle of slope
            (perpendicularNormal, groundNormal, slopeAngle) = SlopeStaticClass.slopify(gameObject, groundLayers, rigid.velocity, slopeCheckObj, groundNormal, onGround(), ref onslope, slopeResults, slopeFilter, getPerp: true, colToUse: currentCol, jumpY: jumpVector.y, forceDownAmount: forceDownAmount);
        }

        moveFunction();

        //total up velocity
        calculatedVel = moveVector + jumpVector + combineOtherVector();
        if (!float.IsNaN(calculatedVel.x) || !float.IsNaN(calculatedVel.y))
        {
            rigid.velocity = calculatedVel;
        }
    }

    private void LateUpdate()
    {
        if ((currentGroundState == groundState.slideing || currentGroundState == groundState.grounded) && onGround() == false && slideGracePeriod <= 0)
        {

            // Vector3 startingScale = slopeCheckObj.transform.localScale;
            // slopeCheckObj.transform.localScale = startingScale * 0.75f;
            if (SlopeStaticClass.slopeFollow(slopeCheckObj, groundLayers, true) == false)
            {
                //if sliding convert yspeed to jump vector
                Debug.Log("cancelling");
                slideGracePeriod = 0.1f;
                jumpVector.y = moveVector.y;
                Debug.Log($"{speedX} * {groundNormal.x} = {groundNormal.x * speedX}");
                speedX = (groundNormal.x * speedX);
                moveVector = new Vector2(speedX, 0);
                speedY = 0;

            }
            else
            {
                Debug.Log("oh come on");
            }
            //slopeCheckObj.transform.localScale = startingScale;
        }
    }
}

[Serializable]
public enum groundState
{
    grounded,
    slideing,
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

[Serializable]
public class otherDictValues
{
    public string key;
    public Vector2 value;
    public bool accelerating = false;
}