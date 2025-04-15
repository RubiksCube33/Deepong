using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleController : MonoBehaviour
{
    private AudioSource sfxSource;
    private ScoreManager scoreManager;
        
    // Start is called before the first frame update
    void Start()
    {
        sfxSource = GetComponent<AudioSource>();
        
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        sfxSource.Play();
        
        if (other.gameObject.CompareTag("Game_Ball") && scoreManager != null)
        {
            scoreManager.AddScore();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
