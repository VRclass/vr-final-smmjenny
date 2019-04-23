using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Rigibody))]
public class float : MonoBehaviour{
    public float waterLevel = 0.0f;
public float floatThreshold = 2.0f;
public float waterDensity = 0.125f;
public float downForce = 4.0f;

float forceFactor;
Vector3 floatForce;

    // Update is called once per frame
    void FixedUpdate (){
    forcerFactor = 1.0f - ((transfrom.position.y - waterLevel) / floatThreshold);

    if (forceFactor > 0.0f){
        floatForce = -Physics.gravity * (forceFactor - GetComponent<Rigibody>().velocity.y * waterDensity);
        floatForce += new Vector3(0.0f, -downForce, 0.0f);
        GetComponent<Rigibody>().AddForceAtPosition(floatForce, Transform.position);
    }
        
    }
}
