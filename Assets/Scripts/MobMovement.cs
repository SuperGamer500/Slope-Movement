using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MobMovement : MonoBehaviour
{
    public float maxSpeed = 10f;
    public float speedX = 0;
    public float speedY = 0;
    private float speed8Dir = 0;
    Vector2 last8Dir;
    public float acceleration = 0.5f;
    public float deccerleration = 0.5f;
    public float skidSpeed = 0.35f;
    [SerializeField] float velPower = 0.5f;

    public groundState currentGroundState;
    [SerializeField] Transform groundCheckTrans;
    [SerializeField] Transform slopeCheckTrans;
    [SerializeField] bool dynamicSlopeObj = true;


    private float slopeAngle;
    [SerializeField] float forceDownAmount = 2.5f;

    Collider2D currentCol;
    (Vector2 rightM, Vector2 leftM, Vector2 middleM, Vector2 rightT, Vector2 leftT, Vector2 middleT, Vector2 rightB, Vector2 leftB, Vector2 middleB) sidesTuple;

    [SerializeField] LayerMask groundLayers = 1 << 0 | 1 << 6 | 1 << 11;
    public float jumpPower = 10;
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
    public bool allowGrav = true;
    public Vector2 otherVector { get; protected set; }
    private bool otherUp;


    public List<otherDictValues> otherDictionary;


    bool wasOnSlope = false;
    private Rigidbody2D rigid;


    public bool rotWithMove = true;

    public Vector2 calculatedVel { get; protected set; } = new Vector2(0, 0);

    public Vector2 requestedDirection;
    public float lockMovement = 0;
    public moveStyle moveType;



    [SerializeField] private Collider2D[] groundResults = new Collider2D[30];
    [SerializeField] private Collider2D[] slopeResults = new Collider2D[30];
    [SerializeField] private Collider2D[] canGoResults = new Collider2D[30];
    private ContactFilter2D groundFilter;
    private ContactFilter2D slopeFilter;
    private ContactFilter2D canGoFilter;

    public bool canFastFall = true;


    Ray2D upRay;

    (bool grav, bool state) groundCall;
    (bool grav, bool state) isOnGround;

    (Ray2D left, Ray2D middle, Ray2D right) rayTuple;


    private Transform trans;
    private Vector3 colSize;

    WaitForFixedUpdate fixedWait = new WaitForFixedUpdate();


    public AllParticles allParticles;
    private bool initialSlopeCheck = false;
    // Start is called before the first frame update
    void Start()
    {
        trans = transform;
        groundFilter.SetLayerMask(groundLayers);

        groundFilter.useTriggers = false;
        slopeFilter.useTriggers = false;
        canGoFilter.useTriggers = false;
        canGoFilter.SetLayerMask(groundLayers);
        if (currentCol == null)
        {
            currentCol = GetComponent<Collider2D>();
            colSize = currentCol.bounds.size;
        }
        if (moveType == moveStyle.normal)
        {
            slopeCheckTrans.gameObject.GetComponent<SpriteRenderer>().enabled = false;
            groundCheckTrans.gameObject.GetComponent<SpriteRenderer>().enabled = false;
        }
        rigid = GetComponent<Rigidbody2D>();

    }
    void changeSlopeObj()
    {
        float speedPlus = (Mathf.Abs(speedX) + Mathf.Abs(speedY)) / (10);
        float yCalc = jumpVector.y > 0.1 ? 0.1f : Mathf.Clamp(0.5f * speedPlus, 0.5f, 2);


        slopeCheckTrans.localScale = jumpVector.y > 0.1 ? new Vector3(currentCol.bounds.size.x, yCalc, 1) : new Vector3(currentCol.bounds.size.x, yCalc, 1);


        slopeCheckTrans.position = sidesTuple.middleB - new Vector2(0, slopeCheckTrans.lossyScale.y / 2);

        forceDownAmount = Mathf.Abs(moveVector.magnitude) / 20;
    }

    #region otherStuff
    Vector2 combineOtherVector()
    {
        otherUp = false;
        Vector2 returnValue = new Vector2();
        for (int i = 0; i < otherDictionary.Count; i++)
        {
            otherDictValues dictValues = otherDictionary[i];
            if (dictValues != null)
            {
                groundNormal = moveType == moveStyle.airborne ? new Vector2(1, 0) : groundNormal;
                returnValue += dictValues.value.x * groundNormal + new Vector2(0, dictValues.value.y);

                if (dictValues.value.y > 0.1f)
                {
                    otherUp = true;
                }
            }
        }
        return returnValue;
    }
    public (bool present, int index) findOther(string forceName)
    {
        (bool boolValue, int intValue) returnValue = (false, -1);
        for (int i = 0; i < otherDictionary.Count; i++)
        {
            otherDictValues dictIndex = otherDictionary[i];
            if (dictIndex.key == forceName)
            {
                returnValue.boolValue = true;
                returnValue.intValue = i;
                break;
            }
        }
        return returnValue;
    }
    public IEnumerator removeOther(string forceName, Vector2 originalValue, float timeToZero = 0, float power = 1)
    {
        otherDictValues testCheck = null;

        for (int i = 0; i < otherDictionary.Count; i++)
        {
            otherDictValues dictIndex = otherDictionary[i];
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
                    yield return fixedWait;
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
    public IEnumerator addOther(string forceName, Vector2 value, OtherMoveType otherMoveType, float timeToMax = 0, float power = 1, bool removeImdiately = false, float removeTime = 0, bool inverse = false)
    {
        otherDictValues testCheck = new otherDictValues();

        bool alreadyThere = false;
        for (int i = 0; i < otherDictionary.Count; i++)
        {
            otherDictValues dictIndex = otherDictionary[i];
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

                    yield return fixedWait;
                    cTime += Time.deltaTime;
                }
            }
            else
            {
                testCheck.value = value;
            }

            if (otherMoveType.untilDirection != 0 || otherMoveType.untilGrounded == true)
            {
                while (true)
                {
                    yield return fixedWait;

                    if (otherMoveType.untilDirection != 0)
                    {
                        if (Mathf.Sign(requestedDirection.x) != Mathf.Sign(otherMoveType.untilDirection) && requestedDirection.x != 0)
                        {
                            break;
                        }
                    }

                    if (otherMoveType.untilGrounded == true && groundCall.grav && jumpVector.y <= 0)
                    {
                        break;
                    }
                }
                StartCoroutine(removeOther(forceName, value, removeTime, power));
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
        if (groundCall.grav == false && otherUp == false)
        {
            isOnGround.state = false;
            float yValue = 0;
            float xValue = 0;

            bool spacePressed = Input.GetKey(KeyCode.Space);

            // if the obj lets go of the jump button early
            if (currentGroundState == groundState.jumping && spacePressed == false && gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                //set state to fastfalling
                currentGroundState = groundState.fastFall;
            }
            //if jumpvector has not reached terminal velocity yet
            if (jumpVector.y > -terminalVelocity)
            {
                //reduce speed by fall values
                if (currentGroundState == groundState.fastFall && canFastFall == true)
                {
                    yValue = Mathf.Max(-terminalVelocity, jumpVector.y - (gravity * Time.deltaTime * fastFallMultiplier));
                }
                else if (currentGroundState == groundState.falling || canFastFall == false)
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
            if (jumpVector.y < 0 && groundCall.state == false && currentGroundState != groundState.fastFall)
            {
                currentGroundState = groundState.falling;
            }
        }
        else
        {
            //if on ground and the player is not jumping
            if (currentGroundState != groundState.jumping)
            {
                jumpVector = new Vector2(0, 0);
            }
        }

        //if on ground and not jumping
        bool falling = currentGroundState == groundState.falling || currentGroundState == groundState.fastFall || currentGroundState == groundState.jumping;

        if (falling && jumpVector.y < 0.1f && groundCall.state)
        {
            canFastFall = true;

            if (allParticles.land)
            {
                allParticles.land.Play();
            }
            currentGroundState = groundState.grounded;

        }
        //removes jumpvector y if the object collides with wall
        #region CornerCheck
        if (jumpVector.y > 0.1f)
        {
            upRay.origin = sidesTuple.middleT;
            upRay.direction = Vector2.up;
            if (Physics2D.Raycast(upRay.origin, upRay.direction, 0.1f, groundLayers))
            {
                jumpVector.y = Mathf.Clamp(jumpVector.y, int.MinValue, 0);
            }
        }

        #endregion

        if ((moveVector.y < -0.1f) && SlopeStaticClass.slopeFollow(slopeCheckTrans, groundLayers, (rayTuple.right, rayTuple.left), true) == false && CollisionStaticFunctions.canGo(trans, transform.position + (Vector3)calculatedVel * Time.deltaTime, currentCol.bounds.size * 0.9f, ref canGoResults, canGoFilter) == true)
        {
            if (wasOnSlope == true)
            {
                //if sliding convert yspeed to jump vector

                jumpVector.y = moveVector.y;
                currentGroundState = groundState.falling;
                moveVector.y = 0;
                speedX = moveVector.x;
                speedY = 0;
            }
        }

        if (moveType == moveStyle.normal)
        {
            moveFunction();
        }
    }
    public void jumpRequest()
    {

        //if object is jumping in the direction of the slope
        if (MathF.Sign(slopeAngle) == Mathf.Sign(requestedDirection.x) && Mathf.Abs(moveVector.x) != 0 && slopeAngle != 0)
        {
            jumpVector = new Vector2(0, 1) * jumpPower + new Vector2(0, moveVector.y / 2);

        }
        else
        {
            //jump based on the slope 

            jumpVector = (perpendicularNormal * 0.5f + new Vector2(0, 1) * 0.5f) * jumpPower;


        }
        currentGroundState = groundState.jumping;
        if (allParticles.jump)
        {
            allParticles.jump.Stop();
            allParticles.jump.Play();
        }
    }
    public bool onGround(string purpose = "state")
    {
        //use for detecting ground states
        if (purpose == "state")
        {
            return !CollisionStaticFunctions.canGo(groundCheckTrans, groundCheckTrans.position, groundCheckTrans.lossyScale, ref groundResults, groundFilter);
        }
        //used for gravity calculations
        else if (purpose == "grav")
        {
            float rot = groundCheckTrans.eulerAngles.z;

            bool left1Check = CollisionStaticFunctions.pointContact(sidesTuple.leftB, Vector2.zero, rot, ref groundResults, groundFilter, currentCol);

            bool left2Check = CollisionStaticFunctions.pointContact(sidesTuple.leftB + new Vector2(0.01f, 0), new Vector2(0, 0.05f), rot, ref groundResults, groundFilter, currentCol);

            if (left1Check == true && left2Check == true)
            {
                return true;
            }

            bool right1Check = CollisionStaticFunctions.pointContact(sidesTuple.rightB, Vector2.zero, rot, ref groundResults, groundFilter, currentCol);

            bool right2Check = CollisionStaticFunctions.pointContact(sidesTuple.rightB - new Vector2(0.01f, 0), new Vector2(0, 0.05f), rot, ref groundResults, groundFilter, currentCol);

            if (right1Check == true && right2Check == true)
            {
                return true;
            }

            return CollisionStaticFunctions.pointContact(sidesTuple.middleB, Vector2.zero, rot, ref groundResults, groundFilter, currentCol);
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

            //dont change requested direction
            input = new Vector2(requestedDirection.x, requestedDirection.y);

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
            float speedToUse = 0;
            incrementCalculate(ref speedToUse, ref speed8Dir, input.magnitude);
        }

        //checks corners to prevent deceleration on walls
        #region CornerCheck

        if (speedX < -0.1f)
        {
            bool leftMCheck = CollisionStaticFunctions.pointContact(sidesTuple.leftM, new Vector2(0, 0), 0, ref canGoResults, canGoFilter, currentCol);

            bool leftTCheck = CollisionStaticFunctions.pointContact(sidesTuple.leftT, new Vector2(0, 0), 0, ref canGoResults, canGoFilter, currentCol);

            bool leftThere = leftMCheck;
            if (leftThere && moveType == moveStyle.normal)
            {
                speedX = Mathf.Max(0, speedX);
            }
        }
        else if (speedX > 0.1f)
        {
            bool rightMCheck = CollisionStaticFunctions.pointContact(sidesTuple.rightM, new Vector2(0, 0), 0, ref canGoResults, canGoFilter, currentCol);

            bool rightTCheck = CollisionStaticFunctions.pointContact(sidesTuple.rightT, new Vector2(0, 0), 0, ref canGoResults, canGoFilter, currentCol);


            bool rightThere = rightMCheck;

            if (rightThere && moveType == moveStyle.normal)
            {
                speedX = Mathf.Min(0, speedX);
            }
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
            if (input == Vector2.zero && speed8Dir != 0)
            {
                Vector2 normalInput = last8Dir.normalized;
                moveVector = new Vector2(normalInput.x * Mathf.Abs(speed8Dir), normalInput.y * Mathf.Abs(speed8Dir));
            }
            else
            {
                Vector2 normalInput = input.normalized;
                moveVector = new Vector2(normalInput.x * Mathf.Abs(speed8Dir), normalInput.y * Mathf.Abs(speed8Dir));
            }

            if (input != Vector2.zero)
            {
                last8Dir = input;
            }
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
        bool accel = true;

        //if input is moving
        if (inputValue != 0)
        {
            accel = true;
        }
        // if input is zero
        else if (inputValue == 0)
        {
            accel = false;
        }

        float increment = 0;
        if (accel == true)
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
        else if (accel == false)
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
        sidesTuple.leftM = rigid.position - new Vector2(colSize.x / 2, 0);
        sidesTuple.rightM = rigid.position + new Vector2(colSize.x / 2, 0);
        sidesTuple.middleM = rigid.position;

        sidesTuple.leftT = sidesTuple.leftM + new Vector2(0, colSize.y / 2);
        sidesTuple.rightT = sidesTuple.rightM + new Vector2(0, colSize.y / 2);
        sidesTuple.middleT = sidesTuple.middleM + new Vector2(0, colSize.y / 2);

        sidesTuple.leftB = sidesTuple.leftM - new Vector2(0, colSize.y / 2);
        sidesTuple.rightB = sidesTuple.rightM - new Vector2(0, colSize.y / 2);
        sidesTuple.middleB = sidesTuple.middleM - new Vector2(0, colSize.y / 2);
    }
    public void setVector()
    {
        calculatedVel = moveVector + jumpVector + combineOtherVector();
        if (!float.IsNaN(calculatedVel.x) || !float.IsNaN(calculatedVel.y))
        {
            rigid.velocity = calculatedVel;
        }
    }
    private void Update()
    {
        //can only be used by the player
        #region playerSection
        if (gameObject.layer == LayerMask.NameToLayer("Player"))
        {


            rotWithMove = !Input.GetKey(KeyCode.LeftShift);


            requestedDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));


            if (moveType == moveStyle.normal)
            {
                bool groundStateCheck = currentGroundState == groundState.grounded;

                bool spacePressed = Input.GetKeyDown(KeyCode.Space);

                //if jump is pressed or the jump buffer is on while on the ground
                if (groundCall.state && (spacePressed || currentJBTime > 0) && groundStateCheck && jumpVector.y < 0.1f)
                {
                    jumpRequest();
                }
                //turn on buffer time if jump isnt satisfied
                else if (spacePressed)
                {
                    currentJBTime = jumpBuffer;
                }
                currentJBTime = Mathf.Clamp(currentJBTime - Time.deltaTime, 0, jumpBuffer);
            }
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.C))
            {
                Time.timeScale = Time.timeScale == 0.3f ? 1 : 0.3f;
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                StartCoroutine(addOther("yay", new Vector2(0, 15), new OtherMoveType(), 1, 0.5f, true, 0.5f));
            }
#endif
        }
        #endregion
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        grabSides();

        lockMovement = Mathf.Max(lockMovement - Time.deltaTime, 0);

        if (dynamicSlopeObj && slopeCheckTrans != null)
        {
            changeSlopeObj();
        }
        //if move style is normal
        if (moveType == moveStyle.normal)
        {
            if (isOnGround.state == false)
            {
                groundCall.state = onGround();
                isOnGround.state = groundCall.state;
            }
            groundCall.grav = groundCall.state == true ? onGround("grav") : false;

            //grab the perpendicular, normal, and angle of slope
            if ((requestedDirection != Vector2.zero || calculatedVel != Vector2.zero) || initialSlopeCheck == false)
            {
                (perpendicularNormal, groundNormal, slopeAngle) = SlopeStaticClass.slopify(trans, groundLayers, rigid.velocity, slopeCheckTrans, rayTuple, ref slopeAngle, perpendicularNormal, groundNormal, groundCall.state, ref wasOnSlope, slopeResults, slopeFilter, colToUse: currentCol, jumpY: jumpVector.y, forceDownAmount: forceDownAmount);
                initialSlopeCheck = true;
            }

            if (allowGrav)
            {
                gravFunction();
            }
        }
        else
        {
            moveFunction();
        }
        //total up velocity
        setVector();
        if (allParticles.walk != null)
        {
            if (rigid.velocity.x != 0 && currentGroundState == groundState.grounded)
            {
                if (allParticles.walk.isPlaying == false)
                {
                    allParticles.walk.Play();
                }
            }
            else if (allParticles.walk.isPlaying == true)
            {
                allParticles.walk.Stop();
            }

        }


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

[Serializable]
public class OtherMoveType
{
    public bool standard { get { return untilDirection != 0 && !untilGrounded; } }
    public float untilDirection;
    public bool untilGrounded;


}

[Serializable]
public class AllParticles
{
    public ParticleSystem walk;
    public ParticleSystem jump;
    public ParticleSystem land;
}

[Serializable]
public class otherDictValues
{
    public string key;
    public Vector2 value;
    public bool accelerating = false;
}