using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{

    [SerializeField] GameObject followObject;
    [SerializeField] Vector2 offset;
    Vector3 velocityRef = Vector3.zero;
    float floatRefX = 0;
    float floatRefY = 0;

    [SerializeField] float xTime = 0.5f;
    [SerializeField] float yTime = 0.5f;

    [SerializeField] float camZoomSize = 10;
    // Start is called before the first frame update
    void Awake()
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (followObject)
        {
            Vector2 newPosition = (Vector2)followObject.transform.position + new Vector2(offset.x, offset.y);


            float smoothX = Mathf.SmoothDamp(transform.position.x, newPosition.x, ref floatRefX, xTime);
            float smoothY = Mathf.SmoothDamp(transform.position.y, newPosition.y, ref floatRefY, yTime);
            transform.position = new Vector3(smoothX, smoothY, -10);
            transform.parent = followObject.transform.parent;
            GetComponent<Camera>().orthographicSize = camZoomSize;
        }

    }
}
