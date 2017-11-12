using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{

    // How fast to spin object
    public float speed = 10f;
    
    void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
}
