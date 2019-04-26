using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour {

    // Use this for initialization

    //show the normals applied to the object
    public bool showNormals=true;

    //custom center of mass
    public Transform COM;

    //these are the floating parameters
    public float floatheigh = 2;
    public float bounceDamp = 0.05f;

    //check the triangles
    public GameObject prefab;

    //these are the internal parameters that are obtained in the script;
    private float forceFactor;
    private Vector3 actionPoint;
    private Vector3 uplift;
    private Rigidbody rb;

    // this is the water plane's script
    public Waves waterScript;
    


    //mesh of this object
    private Mesh objectMesh;


    //arrays used to know the position of the triangles in the mesh
    private Vector3[] trianglePoints, triangleNormals;
    private float[] triangleSurface;
    float totalSurface;

    
    void Start () {
        rb = GetComponent<Rigidbody>();
        objectMesh = transform.GetComponent<MeshFilter>().mesh;

        //change center of mass to custom
        rb.centerOfMass = COM.position;
        StartCoroutine( applyFloatingFOrce());
    }


    // this shows the points where the forces are applied
    IEnumerator applyFloatingFOrce()
    {
        GameObject[] go = new GameObject[objectMesh.triangles.Length/3];

        for (int i = 0; i < objectMesh.triangles.Length/3; i++)
        {
            go[i] = GameObject.Instantiate(prefab,new Vector3(0,0,0), Quaternion.Euler(0,0,0))  as GameObject;
            go[i].transform.parent = transform;

            if(showNormals==false)
            {
                go[i].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            }
        }

        //these arrays are used to determine the points and normal direction where the forces are applied
        trianglePoints = new Vector3[objectMesh.triangles.Length / 3];
        triangleNormals = new Vector3[objectMesh.triangles.Length / 3];
        triangleSurface = new float[objectMesh.triangles.Length / 3];

        totalSurface = 0;

        //we loop according to the triangle mesh
        for (int i = 0; i < objectMesh.triangles.Length / 3; i++)
        {
            //vertex points attached to the triangle
            Vector3 P1 = objectMesh.vertices[objectMesh.triangles[i * 3 + 0]];
            Vector3 P2 = objectMesh.vertices[objectMesh.triangles[i * 3 + 1]];
            Vector3 P3 = objectMesh.vertices[objectMesh.triangles[i * 3 + 2]];
            //center of the triangle
            Vector3 center = ((P1 + P2 + P3) / 3);

            //normals
            Vector3 n1 = objectMesh.normals[objectMesh.triangles[i * 3 + 0]];
            Vector3 n2 = objectMesh.normals[objectMesh.triangles[i * 3 + 1]];
            Vector3 n3 = objectMesh.normals[objectMesh.triangles[i * 3 + 2]];
            //mean normal
            Vector3 faceNormal = ((n1 + n2 + n3) / 3);

            trianglePoints[i] = center;
            triangleNormals[i] = faceNormal;

            //set the new position 
            go[i].transform.position = center + transform.position;
            go[i].transform.right = faceNormal;

            //surface of each triangle used to obtain pressure force
            triangleSurface[i] = 0.5f * Vector3.Cross(P1-P2,P3-P2).magnitude;
            totalSurface += triangleSurface[i];

            // set the local scale proportional to the size of the triangle
            go[i].transform.localScale = new Vector3(triangleSurface[i],1,1);

        }


        // this is the floating part
        while (true)
        {

            if (waterScript.vertices!=null)
            {
                for (int i = 0; i < objectMesh.triangles.Length / 3; i++)
                {
                    actionPoint = go[i].transform.position;

                    //we seek the point on the water closest to the actionPoint considering only X and Z (then Y projection is used as waterlevel)
                    float distMin = 1e30f;
                    int indxMin = 0;

                    Vector3 v1 = new Vector3(actionPoint.x, 0, actionPoint.z);
                    for (int j = 0; j < waterScript.vertices.Length; j++)
                    {
                        Vector3 v2 = new Vector3(waterScript.vertices[j].x*waterScript.transform.localScale.x, 0, waterScript.vertices[j].z * waterScript.transform.localScale.z);

                        float sqrDistance = (v1 - v2).sqrMagnitude;
                        if (sqrDistance < distMin)
                        {
                            distMin = sqrDistance;
                            indxMin = j;

                        }

                    }

                    //the waterlevel is the one of the closest water point in the y projection
                    float waterlevel = waterScript.vertices[indxMin].y * waterScript.transform.localScale.y;


                    forceFactor = ((-actionPoint.y + waterlevel)/floatheigh);

                    uplift = new Vector3(0, 0, 0);
                    if (forceFactor > 0f)
                    {
                        uplift = -Physics.gravity * (forceFactor) * triangleSurface[i] / totalSurface - rb.velocity*bounceDamp;

                        rb.AddForceAtPosition(uplift, actionPoint);
                    }

                    go[i].transform.localScale = new Vector3(uplift.magnitude, 1, 1);
                }

            }        

            yield return null;


        }
    }


   

    // Update is called once per frame
    void FixedUpdate()
    {


    }

}
