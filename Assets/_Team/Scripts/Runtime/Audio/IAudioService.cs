using UnityEngine;

/// <summary>
/// Abstraction for the audio playback subsystem (Dependency Inversion Principle).
/// Allows gameplay and UI systems to trigger audio without being tightly coupled
/// to a concrete AudioManager implementation.
/// </summary>
public interface IAudioService
{
    /// <summary>Plays a music track, performing a crossfade if another track is already playing.</summary>
    void PlayMusic(AudioClip clip, float fadeDuration = 1.5f, bool loop = true);

    /// <summary>Smoothly fades out the currently playing music track.</summary>
    void FadeOutMusic(float fadeDuration = 1.5f);

    /// <summary>Immediately stops all playing music.</summary>
    void StopMusic();

    /// <summary>Plays a 2D sound effect.</summary>
    void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f);

    /// <summary>Plays a 3D sound effect at a given world position.</summary>
    void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f, float pitchVariance = 0f);

    /// <summary>Updates volume levels in real-time (0.0 to 1.0).</summary>
    void SetMasterVolume(float volume);
    void SetSFXVolume(float volume);
    void SetMusicVolume(float volume);
}

