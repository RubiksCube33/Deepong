using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleController : MonoBehaviour
{
    private AudioSource sfxSource;
        
    // Start is called before the first frame update
    void Start()
    {
        sfxSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision other)
    {
        sfxSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
