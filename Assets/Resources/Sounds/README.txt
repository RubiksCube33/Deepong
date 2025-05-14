Sound Resources Directory Structure
============================

This directory contains all audio resources for the game:

1. Music/ - Contains background music tracks
   - Used for continuous background music during gameplay and menus
   - Recommended format: MP3 or OGG (44.1kHz, 160-320kbps)
   - Example file: "Digital Horizons.mp3"

2. SFX/ - Contains sound effects
   - Used for UI interactions, gameplay events, and feedback
   - Recommended format: WAV (44.1kHz, 16-bit)
   - Example files: "ButtonClick.wav", "PaddleHit.wav", "Score.wav"

How to Use:
----------
1. Place audio files in the appropriate folders
2. The SoundManager automatically loads all audio files from these directories
3. Play sounds in code using:
   - SoundManager.Instance.PlayMusic("MusicFileName") - without file extension
   - SoundManager.Instance.PlaySFX("SFXFileName") - without file extension

Notes:
-----
- Ensure audio filenames don't contain spaces or special characters
- Keep SFX files short and optimized for quick loading
- For looping sounds, set loop=true in the audio source 