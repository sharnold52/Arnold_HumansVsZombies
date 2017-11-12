using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{

    // radius and position
    public float radius;
    public Vector3 position;

	// Use this for initialization
	void Start ()
    {
        // get position
        position = gameObject.transform.position;
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
