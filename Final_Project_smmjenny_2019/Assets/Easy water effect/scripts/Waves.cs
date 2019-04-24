using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Waves : MonoBehaviour {


    //this are the variables used for the waves parameters:
    public float waveHeight = 0.31f;
	public float speed = 1.18f;
	public float waveLength = 2f;
	public float randomHeight = 0.01f;
	public float randomSpeed = 9f;
	public float noiseOffset = 20.0f;
    public Vector3[] vertices;

    // these private variables in the corrutine function
    private Vector3[] baseHeight;
	private List<float> perVertexRandoms = new List<float>();
	private Mesh mesh;
    private MeshCollider meshcollider;

	
	void Awake() {

        // this is the mesh (in this case a plane)
		mesh = GetComponent<MeshFilter>().mesh;
        meshcollider = GetComponent<MeshCollider>();

        //this is the mesh collider

        if (baseHeight == null) {
			baseHeight = mesh.vertices;
		}

        //this is the random initial configuration of the vertex
		for(int i=0; i < baseHeight.Length; i++) {
			perVertexRandoms.Add(Random.value * randomHeight);
		}


        StartCoroutine(drawWaves());
	}


    //this generates the movement of the waves as a corrutine
    IEnumerator drawWaves()
    {
        while (true)
        {
            vertices = new Vector3[baseHeight.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                // the vertex has three components, but ony the Y value is modified to get the wave effect
                Vector3 vertex = baseHeight[i];
                
                //we initialise the random value according to the following value:
                Random.InitState((int)((vertex.x + noiseOffset) * (vertex.x + noiseOffset) + (vertex.z + noiseOffset) * (vertex.z + noiseOffset)));

                // the vertex Y value follows a sinusoid  that has two contributions:
                    //first contribution "sinusoid"
                vertex.y += Mathf.Sin(Time.time * speed + baseHeight[i].x * waveLength + baseHeight[i].y * waveLength) * waveHeight;
                    //second contribution "random"
                vertex.y += Mathf.Sin(Mathf.Cos(Random.value * 1.0f) * randomHeight * Mathf.Cos(Time.time * randomSpeed * Mathf.Sin(Random.value * 1.0f)));
         
                // these are the new vertices
                vertices[i] = vertex;
            }
            yield return null;

            // in this case we set the vertices to the mesh and we update the normal direction
            mesh.vertices = vertices;
            mesh.RecalculateNormals();

            //then we update the meshcollider
            meshcollider.sharedMesh = mesh;
        }
    }


	void Update ()
    {

        

    }
}