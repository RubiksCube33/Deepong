using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallController : MonoBehaviour
{
    private Rigidbody rb;

    private float randX;
    private float randY;
    private float randZ;
    
    // Start is called before the first frame update
    void Start()
    {
        randX = Random.Range(0f, 10f);
        randY = Random.Range(0f, 10f);
        randZ = Random.Range(0f, 10f);
        
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(randX, randY, randZ);
    }

    void OnCollisionExit(Collision collision)
    {
        Vector3 currentVelocity = rb.velocity;
        rb.velocity = currentVelocity * 1.1f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
