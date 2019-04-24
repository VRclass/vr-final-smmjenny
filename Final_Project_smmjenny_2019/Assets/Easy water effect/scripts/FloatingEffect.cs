using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour {

    // Use this for initialization

    //show the normals applied to the object
    public bool showNormals=true;

    //custom center of mass
    //public Transform COM;

    //these are the floating parameters
    public float floatheight = 2;

    //boats
    public GameObject[] boats;

    //check the triangles
    public GameObject prefab;

    //these are the internal parameters that are obtained in the script;
    private float forceFactor;
    private Vector3 actionPoint;
    private Vector3 uplift;
    private Rigidbody[] rb;

    // this is the water plane's script
    public Waves waterScript;

    //compute shader for floating effect
    public ComputeShader floatingShader;
    ComputeBuffer trianglePos, waterVertice, triangleSurfaceBuffer;
    ComputeBuffer outForce;

    //mesh of this object
    private Mesh[] objectMesh;

    public float density=100;

    //arrays used to know the position of the triangles in the mesh
    private Vector3[][] trianglePoints, triangleNormals;
    private float[][] triangleSurface;
    float[] totalSurface;

    //game objects
    GameObject[][] go;
    //force
    Vector3[][] f;

    // number of triangles of the gameobject "x"
    int[] nb_tria;

    /// <summary>
    ///  IN THIS PART WE INITIALISE THE VARIABLES AND GET THE TRIANGLES OF EACH MESH OF EVERY FLOATING OBJECT
    /// </summary>
    void Start () {

        //we get the initial rotation in order to re-do the mesh properly
        Quaternion[] initialRot =new Quaternion[boats.Length];

        //initialise variables
        nb_tria = new int[boats.Length];
        rb =new Rigidbody[boats.Length];
        objectMesh = new Mesh[boats.Length];
        go =new GameObject[boats.Length][];

        //for each floating object (boat)
        for (int j = 0;j< boats.Length; j++)
        {
            //reset rotation to zero and store value for future use
            initialRot[j] =boats[j].transform.rotation;
            boats[j].transform.rotation = Quaternion.Euler(0, 0, 0);

            rb[j] =boats[j].transform.GetComponent<Rigidbody>();
            objectMesh[j] = boats[j].transform.GetComponent<MeshFilter>().mesh;
            nb_tria[j] = objectMesh[j].triangles.Length / 3;
            go[j] = new GameObject[nb_tria[j]];
            
        }

        //initialise variables
        trianglePoints = new Vector3[boats.Length][];
        triangleNormals = new Vector3[boats.Length][];
        totalSurface = new float[boats.Length];
        triangleSurface=new float[boats.Length][];

        f = new Vector3[boats.Length][];

        for (int j = 0; j < boats.Length; j++)
        {
            f[j] = new Vector3[nb_tria[j]];
        }

        //for each floating object
        for (int j = 0; j < boats.Length; j++)
        {
            //for each of the triangles in the mesh
            for (int i = 0; i <  nb_tria[j]; i++)
            {
                //instanciate the cube that represents the force vector on each triangle
                go[j][i] = GameObject.Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.Euler(0, 0, 0)) as GameObject;
                go[j][i].transform.parent = boats[j].transform;

                if (showNormals == false)
                {
                    go[j][i].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                }
            }

            //these arrays are used to determine the points and normal direction where the forces are applied
            trianglePoints[j] = new Vector3[ nb_tria[j]];
            triangleNormals[j] = new Vector3[ nb_tria[j]];
            triangleSurface[j] = new float[ nb_tria[j]];
            
            //reset the total surface of the element j to zero
            totalSurface[j] = 0;

            //we loop according to the triangle mesh
            for (int i = 0; i <  nb_tria[j]; i++)
            {
                //vertex points attached to the triangle
                Vector3 P1 = objectMesh[j].vertices[objectMesh[j].triangles[i * 3 + 0]];
                Vector3 P2 = objectMesh[j].vertices[objectMesh[j].triangles[i * 3 + 1]];
                Vector3 P3 = objectMesh[j].vertices[objectMesh[j].triangles[i * 3 + 2]];
                //center of the triangle
                Vector3 center = ((P1 + P2 + P3) / 3);
                //normals
                Vector3 n1 = objectMesh[j].normals[objectMesh[j].triangles[i * 3 + 0]];
                Vector3 n2 = objectMesh[j].normals[objectMesh[j].triangles[i * 3 + 1]];
                Vector3 n3 = objectMesh[j].normals[objectMesh[j].triangles[i * 3 + 2]];
                //mean normal
                Vector3 faceNormal = ((n1 + n2 + n3) / 3);
                
                //these are the values that will be used in the Shader for the position of the center of each triangle
                trianglePoints[j][i] = center;
                triangleNormals[j][i] = faceNormal;

                //set the new position 
                go[j][i].transform.position = center + boats[j].transform.position;
                go[j][i].transform.right = faceNormal;

                //surface of each triangle used to obtain pressure force
                triangleSurface[j][i] = 0.5f * Vector3.Cross(P1 - P2, P3 - P2).magnitude;
                totalSurface[j] += triangleSurface[j][i];

                // set the local scale proportional to the size of the triangle (initial condition)
                go[j][i].transform.localScale = new Vector3(triangleSurface[j][i], 1, 1);

            }
            
            //reset the initial rotation of the gameobject
            boats[j].transform.rotation= initialRot[j];


        }
        
        //main tread
        StartCoroutine(calculateFloatingForce());
    }

    /// <summary>
    /// IN THIS PART WE RELEASE THE BUFFERS
    /// </summary>
    private void OnApplicationQuit()
    {
        //release the buffers after
        trianglePos.Release();
        waterVertice.Release();
        outForce.Release();
        triangleSurfaceBuffer.Release();
     

    }

    /// <summary>
    /// IN THIS PART WE CREATE THE BUFFERS FOR THE SHADER AND THE SHADER CALCULATES THE FORCE OF EACH TRIANGLE IN THE MESH OF EVERY FLOATING OBJECT
    /// </summary>
    // this shows the points where the forces are applied
    IEnumerator calculateFloatingForce()
    {

        // this is the floating part
        while (true)
        {
           
            //for each of the boats / floating objects
            for (int j = 0; j < boats.Length; j++)
            {
              
                //buffers
                trianglePos = new ComputeBuffer(nb_tria[j], 3 * sizeof(float));
                triangleSurfaceBuffer = new ComputeBuffer(nb_tria[j], sizeof(float));
                waterVertice = new ComputeBuffer(waterScript.vertices.Length, 3 * sizeof(float));
                outForce = new ComputeBuffer(nb_tria[j], 3 * sizeof(float));

                // initialize the buffers
                // use temporary vectors and floats 
                Vector3[] inputTempP = new Vector3[nb_tria[j]];
                float[] inputTempS = new float[nb_tria[j]];
                Vector3[] inputZeros = new Vector3[nb_tria[j]];

                //for each of the triangles, copy the data to the temp variable
                //Debug.Log("j========="+j);
                for (int ii = 0; ii < nb_tria[j]; ii++)
                {
                    //Debug.Log("i=" + ii);
                    inputTempP[ii] = go[j][ii].transform.position;
                    inputZeros[ii] = new Vector3(0, 0, 0);
                    inputTempS[ii] = triangleSurface[j][ii];

                    //Debug.Log(inputTempP[ii]);

                }

                //set the data to the buffer
                trianglePos.SetData(inputTempP);
                triangleSurfaceBuffer.SetData(inputTempS);
                outForce.SetData(inputZeros);

                //do the same for the water vertecies
                Vector3[] inputTempT = new Vector3[waterScript.vertices.Length];
                for (int ii = 0; ii < waterScript.vertices.Length; ii++)
                {
                    inputTempT[ii] = waterScript.vertices[ii];
                }
                waterVertice.SetData(inputTempT);
                

                f[j] = new Vector3[nb_tria[j]];

                    
                    //LAUNCH SHADER
                    int kernelHandle4 = floatingShader.FindKernel("CSMain");
                    floatingShader.SetBuffer(kernelHandle4, "ActPoint", trianglePos);
                    floatingShader.SetBuffer(kernelHandle4, "Force", outForce);
                    floatingShader.SetBuffer(kernelHandle4, "Wvert", waterVertice);
                    floatingShader.SetBuffer(kernelHandle4, "triangleSurface", triangleSurfaceBuffer);

                    floatingShader.SetInt("n_waterVertices", waterScript.vertices.Length);
                    floatingShader.SetFloat("localSx", waterScript.transform.localScale.x);
                    floatingShader.SetFloat("localSy", waterScript.transform.localScale.y);
                    floatingShader.SetFloat("localSz", waterScript.transform.localScale.z);
                    floatingShader.SetFloat("density", density);
                    floatingShader.SetFloat("floatheight", floatheight);
                    floatingShader.SetFloat("totalSurface", totalSurface[j]);


                    
                    floatingShader.Dispatch(kernelHandle4, nb_tria[j], 1, 1);

                    
                    //RECOVER DATA
                    outForce.GetData(f[j]);

                
                    yield return null;  

                    //release the buffers
                    trianglePos.Release();
                    waterVertice.Release();
                    outForce.Release();
                    triangleSurfaceBuffer.Release();
                                   

                        
             
            }

        }
    }



    private void FixedUpdate()
    {
        //set forces and scales to the objects
        
        for (int j = 0; j < boats.Length; j++)
        {
            for (int ii = 0; ii < nb_tria[j]; ii++)
            {
                rb[j].AddForceAtPosition(f[j][ii], go[j][ii].transform.position);
                //rb[j].AddForceAtPosition(new Vector3(1,1,1), go[j][ii].transform.position);
                go[j][ii].transform.localScale = new Vector3(f[j][ii].magnitude, 1, 1);
            }

            if (j == 0)
            {
                /*Debug.Log("j=" + j + "----------------");
                for (int ii = 0; ii < nb_tria[j]; ii++)
                {
                    Debug.Log("i=" + ii + " f=" + f[j][ii]);
                }*/
            }

        }



    }


}
