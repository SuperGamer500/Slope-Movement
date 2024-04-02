using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

static public class CollisionStaticFunctions
{
    public static bool canGo(Transform objTrans, Vector3 nextPosition, Vector2 scale, ref Collider2D[] results, ContactFilter2D filter)
    {
        for (int i = 0; i < results.Length; i++)
        {
            results.SetValue(null, i);
        }
        Physics2D.OverlapBox(nextPosition, scale, objTrans.eulerAngles.z, filter, results);
        for (int i = 0; i < results.Length; i++)
        {
            Collider2D col = results[i];
            if (col != null)
            {
                if (objTrans.gameObject != col.gameObject)
                {
                    return false;
                }
                results.SetValue(null, i);
            }
        }
        return true;
    }



    public static bool pointContact(Vector2 pos, Vector2 scale, float rot, ref Collider2D[] results, ContactFilter2D filter, Collider2D currentCol)
    {
        for (int i = 0; i < results.Length; i++)
        {
            results.SetValue(null, i);
        }
        Physics2D.OverlapBox(pos, scale, rot, filter, results);
        for (int i = 0; i < results.Length; i++)
        {
            Collider2D col = results[i];
            if (col != null)
            {

                if (col.gameObject != currentCol.gameObject)
                {
                    results.SetValue(null, i);
                    return true;
                }
                results.SetValue(null, i);
            }
        }
        return false;
    }


}
