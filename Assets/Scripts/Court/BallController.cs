using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    private float speed = 150f;
    private Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (rb.velocity == Vector3.zero)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 hitDirection = (collision.transform.position - contact.point).normalized;
            Vector3 forceDirection = hitDirection;
            float forceMagnitude = 25.0f;
        
            rb.velocity = forceDirection * forceMagnitude;
        }
        
        Color newColor = new Color(Random.value, Random.value, Random.value);
        rend.material.color = newColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
