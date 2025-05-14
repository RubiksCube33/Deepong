using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSoundController : MonoBehaviour
{
    [SerializeField] private string backgroundMusicName = "Digital Horizons";
    
    private void Start()
    {
        // Play background music when the main menu loads
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(backgroundMusicName);
        }
        else
        {
            Debug.LogWarning("SoundManager instance not found. Make sure it's in the scene.");
        }
    }
    
    // Example method to play button click sound
    public void PlayButtonClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("ButtonClick");
        }
    }
} 