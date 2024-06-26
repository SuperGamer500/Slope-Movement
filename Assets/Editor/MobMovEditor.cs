
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


[CustomEditor(typeof(MobMovement))]
public class MobMovEditor : Editor
{
    #region SerializedProperties
    SerializedProperty maxSpeed;
    SerializedProperty speedX;
    SerializedProperty speedY;
    SerializedProperty acceleration;
    SerializedProperty deccerleration;
    SerializedProperty skidSpeed;
    SerializedProperty velPower;

    SerializedProperty currentGroundState;
    SerializedProperty groundCheckTrans;


    SerializedProperty slopeCheckTrans;
    SerializedProperty dynamicSlopeObj;

    SerializedProperty groundLayers;

    SerializedProperty jumpPower;
    SerializedProperty gravity;

    SerializedProperty terminalVelocity;
    SerializedProperty airResitance;
    SerializedProperty normalFallMultiplier;
    SerializedProperty fastFallMultiplier;
    SerializedProperty jumpBuffer;





    SerializedProperty moveVector;
    SerializedProperty jumpVector;

    SerializedProperty requestedDirection;



    SerializedProperty forceDownAmount;
    SerializedProperty moveType;

    SerializedProperty rotWithMove;

    SerializedProperty otherDictionary;
    SerializedProperty mobileSupport;
    SerializedProperty allParticles;


    #endregion




    bool speedButton = false;
    bool jumpButton = false;
    bool vectorButton = false;

    bool checkButton = false;
    bool particleButton = false;


    private void OnEnable()
    {
        maxSpeed = serializedObject.FindProperty("maxSpeed");
        speedX = serializedObject.FindProperty("speedX");
        speedY = serializedObject.FindProperty("speedY");
        acceleration = serializedObject.FindProperty("acceleration");
        deccerleration = serializedObject.FindProperty("deccerleration");
        skidSpeed = serializedObject.FindProperty("skidSpeed");
        velPower = serializedObject.FindProperty("velPower");

        currentGroundState = serializedObject.FindProperty("currentGroundState");
        groundCheckTrans = serializedObject.FindProperty("groundCheckTrans");

        slopeCheckTrans = serializedObject.FindProperty("slopeCheckTrans");
        dynamicSlopeObj = serializedObject.FindProperty("dynamicSlopeObj");

        forceDownAmount = serializedObject.FindProperty("forceDownAmount");

        groundLayers = serializedObject.FindProperty("groundLayers");
        jumpPower = serializedObject.FindProperty("jumpPower");
        gravity = serializedObject.FindProperty("gravity");

        terminalVelocity = serializedObject.FindProperty("terminalVelocity");
        airResitance = serializedObject.FindProperty("airResitance");
        normalFallMultiplier = serializedObject.FindProperty("normalFallMultiplier");
        fastFallMultiplier = serializedObject.FindProperty("fastFallMultiplier");
        jumpBuffer = serializedObject.FindProperty("jumpBuffer");





        moveVector = serializedObject.FindProperty("moveVector");
        jumpVector = serializedObject.FindProperty("jumpVector");
        requestedDirection = serializedObject.FindProperty("requestedDirection");
        moveType = serializedObject.FindProperty("moveType");

        rotWithMove = serializedObject.FindProperty("rotWithMove");

        otherDictionary = serializedObject.FindProperty("otherDictionary");

        mobileSupport = serializedObject.FindProperty("mobileSupport");

        allParticles = serializedObject.FindProperty("allParticles");







    }
    void speedSection()
    {
        speedButton = EditorGUILayout.BeginFoldoutHeaderGroup(content: "Speed Values", foldout: speedButton);
        if (speedButton)
        {
            EditorGUILayout.BeginHorizontal();
            float currentWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 75;
            EditorGUILayout.PropertyField(label: new GUIContent("Speed X"), property: speedX);
            EditorGUILayout.PropertyField(label: new GUIContent("Speed Y"), property: speedY);
            EditorGUIUtility.labelWidth = currentWidth;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(label: new GUIContent("Max Speed"), property: maxSpeed);
            EditorGUILayout.PropertyField(label: new GUIContent("Acceleration"), property: acceleration);
            EditorGUILayout.PropertyField(label: new GUIContent("Decceleration"), property: deccerleration);
            EditorGUILayout.PropertyField(label: new GUIContent("Skid Speed"), property: skidSpeed);
            EditorGUILayout.PropertyField(label: new GUIContent("Speed Build Up"), property: velPower);


            if (((MobMovement)target).gameObject.CompareTag("Enemy"))
            {
                EditorGUILayout.PropertyField(label: new GUIContent("Rotate with Movement"), property: rotWithMove);
            }
            else
            {
                EditorGUILayout.PropertyField(label: new GUIContent("Mobile Script"), property: mobileSupport);
            }

        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void jumpSection()
    {
        jumpButton = EditorGUILayout.BeginFoldoutHeaderGroup(content: "Jump Values", foldout: jumpButton);
        if (jumpButton)
        {
            if (moveType.enumValueIndex == (int)moveStyle.normal)
            {
                EditorGUILayout.PropertyField(label: new GUIContent("Jump Power"), property: jumpPower);
                EditorGUILayout.PropertyField(label: new GUIContent("Gravity"), property: gravity);
                EditorGUILayout.PropertyField(label: new GUIContent("Terminal Velocity"), property: terminalVelocity);
                EditorGUILayout.PropertyField(label: new GUIContent("Normal Fall Speed"), property: normalFallMultiplier);
                EditorGUILayout.PropertyField(label: new GUIContent("Fast Fall Speed"), property: fastFallMultiplier);
                EditorGUILayout.PropertyField(label: new GUIContent("Jump Buffer"), property: jumpBuffer);
                EditorGUILayout.PropertyField(label: new GUIContent("Air Resistance"), property: airResitance);
                EditorGUILayout.PropertyField(label: new GUIContent("Ground State"), property: currentGroundState);
            }

            EditorGUILayout.PropertyField(label: new GUIContent("Ground Layers"), property: groundLayers);


        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void vectorSection()
    {
        vectorButton = EditorGUILayout.BeginFoldoutHeaderGroup(content: "Vector Values", foldout: vectorButton);
        if (vectorButton)
        {
            EditorGUILayout.PropertyField(label: new GUIContent("Move"), property: moveVector);
            EditorGUILayout.PropertyField(label: new GUIContent("Jump"), property: jumpVector);
            EditorGUILayout.PropertyField(label: new GUIContent("Requested Direction"), property: requestedDirection);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void checkSection()
    {
        checkButton = EditorGUILayout.BeginFoldoutHeaderGroup(content: "checkObjs", foldout: checkButton);
        if (checkButton)
        {
            EditorGUILayout.PropertyField(label: new GUIContent("Ground Check"), property: groundCheckTrans);


            EditorGUILayout.PropertyField(label: new GUIContent("Slope Check Obj"), property: slopeCheckTrans);

            EditorGUILayout.PropertyField(label: new GUIContent("Dynamic Slope Check"), property: dynamicSlopeObj);

            EditorGUILayout.PropertyField(label: new GUIContent("Force Down Amount"), property: forceDownAmount);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void otherDictSection()
    {
        EditorGUILayout.PropertyField(label: new GUIContent("Other Dictionary"), property: otherDictionary);
    }

    void particleSection()
    {

        EditorGUILayout.PropertyField(label: new GUIContent("All Particles"), property: allParticles);


    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(label: new GUIContent("Move Style"), property: moveType);

        speedSection();


        jumpSection();




        vectorSection();

        if (moveType.enumValueIndex == (int)moveStyle.normal)
        {
            checkSection();
        }



        otherDictSection();

        particleSection();



        serializedObject.ApplyModifiedProperties();
    }
}
