using UnityEngine;

/// <summary>
/// Modular data container for sound effects.
/// Supports multiple random clips (e.g. footsteps, UI clicks) and subtle pitch variance for organic feel.
/// </summary>
[CreateAssetMenu(fileName = "NewAudioCue", menuName = "Audio/AudioCue", order = 1)]
public class AudioCue : ScriptableObject
{
    [Tooltip("List of clips to pick from at random. If only 1 is assigned, it will always play that one.")]
    [SerializeField] private AudioClip[] m_clips;

    [Tooltip("Base playback volume (0 to 1).")]
    [SerializeField, Range(0f, 1f)] private float m_volume = 1f;

    [Tooltip("Random pitch variation applied on each playback (e.g. 0.05 gives 0.95 to 1.05).")]
    [SerializeField, Range(0f, 0.5f)] private float m_pitchVariance = 0.05f;

    public float Volume => m_volume;
    public float PitchVariance => m_pitchVariance;

    /// <summary>Returns a random clip from the assigned collection.</summary>
    public AudioClip GetRandomClip()
    {
        if (m_clips == null || m_clips.Length == 0) return null;
        if (m_clips.Length == 1) return m_clips[0];
        return m_clips[Random.Range(0, m_clips.Length)];
    }

    /// <summary>Convenience method to play this cue through the AudioManager.</summary>
    public void Play()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudioCue(this);
        }
    }

    /// <summary>Convenience method to play this cue in 3D world space through the AudioManager.</summary>
    public void PlayAt(Vector3 position)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAudioCueAt(this, position);
        }
    }
}

