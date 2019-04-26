using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFloating : MonoBehaviour
{

    // Use this for initialization

    //these are the floating parameters
    public float waterlevel = 2;
    public float floatheigh = 2;
    public float bounceDamp = 0.05f;
    public Vector3 offsetP;

    //these are the internal parameters that are obtained in the script;
    private float forceFactor;
    private Vector3 actionPoint;
    private Vector3 uplift;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        offsetP = new Vector3((float)Random.Range(0, 30)/1000, (float)Random.Range(0, 30)/1000, (float)Random.Range(0, 30)/1000);

    }


  
    // Update is called once per frame
    void FixedUpdate()
    {

        //the action point considers the offset of the object in local space
        actionPoint = transform.position+transform.TransformDirection(offsetP);

        //the force factor is a value between 0 and 1
        forceFactor = 1.0f - ((actionPoint.y - waterlevel) / floatheigh);

        //only when the force applied is greatter than zero 
        if(forceFactor>0f)
        {
            uplift = -Physics.gravity * (forceFactor - rb.velocity.y * bounceDamp);

            rb.AddForceAtPosition(uplift, actionPoint);
        }

    }

        
}