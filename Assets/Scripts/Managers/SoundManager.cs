using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.5f;
    
    private Dictionary<string, AudioClip> musicClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    
    private string currentMusic = "";
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize audio sources if not set in inspector
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
        
        // Load all audio clips from Resources
        LoadAudioClips();
    }
    
    private void LoadAudioClips()
    {
        // Load music clips
        AudioClip[] loadedMusicClips = Resources.LoadAll<AudioClip>("Sounds/Music");
        foreach (AudioClip clip in loadedMusicClips)
        {
            if (!musicClips.ContainsKey(clip.name))
            {
                musicClips.Add(clip.name, clip);
                Debug.Log($"Loaded music: {clip.name}");
            }
        }
        
        // Load SFX clips
        AudioClip[] loadedSfxClips = Resources.LoadAll<AudioClip>("Sounds/SFX");
        foreach (AudioClip clip in loadedSfxClips)
        {
            if (!sfxClips.ContainsKey(clip.name))
            {
                sfxClips.Add(clip.name, clip);
                Debug.Log($"Loaded SFX: {clip.name}");
            }
        }
    }
    
    // Music Methods
    public void PlayMusic(string musicName)
    {
        if (currentMusic == musicName) return;
        
        if (musicClips.TryGetValue(musicName, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.Play();
            currentMusic = musicName;
            Debug.Log($"Playing music: {musicName}");
        }
        else
        {
            Debug.LogWarning($"Music clip not found: {musicName}");
        }
    }
    
    public void StopMusic()
    {
        musicSource.Stop();
        currentMusic = "";
    }
    
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }
    
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying)
            musicSource.UnPause();
    }
    
    // SFX Methods
    public void PlaySFX(string sfxName)
    {
        if (sfxClips.TryGetValue(sfxName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
            Debug.Log($"Playing SFX: {sfxName}");
        }
        else
        {
            Debug.LogWarning($"SFX clip not found: {sfxName}");
        }
    }
    
    // Volume Control
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
    
    public float GetMusicVolume()
    {
        return musicVolume;
    }
    
    public float GetSFXVolume()
    {
        return sfxVolume;
    }
    
    // Load saved volume settings
    private void LoadVolumeSettings()
    {
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume"));
        }
        
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        }
    }
    
    private void OnEnable()
    {
        LoadVolumeSettings();
    }
} 