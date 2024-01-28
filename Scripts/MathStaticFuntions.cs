using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.VisualScripting;


public static class MathStaticFuntions
{

    public static float getAngle(float xValue, float yValue, bool inDegrees = true, bool alwaysPositive = false)
    {
        float returnValue = Mathf.Atan2(yValue, xValue);

        if (alwaysPositive == true && Mathf.Sign(returnValue) == -1)
        {
            //returnValue *= -1;
            returnValue += 3.14159f * 2;
        }
        returnValue *= inDegrees ? Mathf.Rad2Deg : 1;


        return returnValue;
    }
}
