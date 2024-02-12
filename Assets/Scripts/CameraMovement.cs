using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    public GameObject followObject;
    private Camera cam { get { return GetComponent<Camera>(); } }
    public Transform lockObj;
    public bool lockX = false;
    public bool lockY = false;


    public Vector2 offset;

    float floatRefX = 0;
    float floatRefY = 0;
    float zoomRef = 0;
    public float xTime = 0.5f;
    public float yTime = 0.5f;
    public float camZoomTime = 0.5f;
    public float camZoomSize = 10;


    // Start is called before the first frame update
    void Start()
    {
        if (followObject)
        {
            transform.position = new Vector3(followObject.transform.position.x, followObject.transform.position.y, -10);
        }

    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (followObject)
        {
            Vector2 newPosition = (Vector2)followObject.transform.position + new Vector2(offset.x, offset.y);

            if (lockObj != null)
            {
                if (lockX)
                {
                    newPosition = new Vector2(lockObj.position.x, newPosition.y);
                }
                if (lockY)
                {
                    newPosition = new Vector2(newPosition.x, lockObj.position.y);
                }
            }

            float smoothX = Mathf.SmoothDamp(transform.position.x, newPosition.x, ref floatRefX, xTime);
            float smoothY = Mathf.SmoothDamp(transform.position.y, newPosition.y, ref floatRefY, yTime);
            transform.position = new Vector3(smoothX, smoothY, -10);
            transform.parent = followObject.transform.parent;


            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, camZoomSize, ref zoomRef, camZoomTime);
        }

    }
}
