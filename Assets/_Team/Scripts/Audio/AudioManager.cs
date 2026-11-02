using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Modular Audio Manager implementing crossfading music, pooled SFX playback,
/// AudioMixer routing, and live volume control integrated with <see cref="GameSettings"/>.
/// Single Responsibility: centralized audio playback and mixing orchestration.
/// </summary>
[DefaultExecutionOrder(-50)]
public class AudioManager : MonoBehaviour, IAudioService
{
    public static AudioManager Instance { get; private set; }

    [Header("Settings Data")]
    [Tooltip("Persistent user settings holding Master, SFX and Music volume sliders.")]
    [SerializeField] private GameSettings m_settings;

    [Header("Audio Mixer & Groups")]
    [Tooltip("Main AudioMixer asset.")]
    [SerializeField] private AudioMixer m_audioMixer;

    [Tooltip("AudioMixerGroup for Master output (optional).")]
    [SerializeField] private AudioMixerGroup m_masterGroup;

    [Tooltip("AudioMixerGroup for Music channels.")]
    [SerializeField] private AudioMixerGroup m_musicGroup;

    [Tooltip("AudioMixerGroup for SFX playback.")]
    [SerializeField] private AudioMixerGroup m_sfxGroup;

    [Header("Mixer Exposed Parameter Names")]
    [Tooltip("Exposed parameter name for Master Volume in the AudioMixer.")]
    [SerializeField] private string m_masterParamName = "MasterVolume";

    [Tooltip("Exposed parameter name for Music Volume in the AudioMixer.")]
    [SerializeField] private string m_musicParamName  = "MusicVolume";

    [Tooltip("Exposed parameter name for SFX Volume in the AudioMixer.")]
    [SerializeField] private string m_sfxParamName    = "SFXVolume";

    [Header("Decibel Limits (dB Boost)")]
    [Tooltip("Maximum volume in dB when Master slider is at 100%. Usually 0dB.")]
    [SerializeField] private float m_maxMasterDb = 0f;

    [Tooltip("Maximum volume in dB when Music slider is at 100%. Set to 10 for +10dB boost.")]
    [SerializeField] private float m_maxMusicDb = 10f;

    [Tooltip("Maximum volume in dB when SFX slider is at 100%. Usually 0dB.")]
    [SerializeField] private float m_maxSfxDb = 0f;

    [Tooltip("Minimum volume in dB when slider is at 0%. -80dB is complete silence.")]
    [SerializeField] private float m_minDb = -80f;

    [Header("Configuration")]
    [Tooltip("If true, this GameObject persists across scene loads.")]
    [SerializeField] private bool m_dontDestroyOnLoad = true;

    [Tooltip("Initial number of pooled SFX audio sources to create.")]
    [SerializeField, Min(4)] private int m_initialSfxPoolSize = 12;

    [Header("Default Music (Optional)")]
    [Tooltip("Optional track to start playing automatically on Start.")]
    [SerializeField] private AudioClip m_playOnStartMusic;
    [SerializeField] private float m_startMusicFadeIn = 1.5f;

    // --- Public Getters for Groups ---
    public AudioMixer Mixer => m_audioMixer;
    public AudioMixerGroup MasterGroup => m_masterGroup;
    public AudioMixerGroup MusicGroup => m_musicGroup;
    public AudioMixerGroup SFXGroup => m_sfxGroup;

    // --- Music Sources (Dual channels for smooth crossfades) ---
    private AudioSource m_musicSourceA;
    private AudioSource m_musicSourceB;
    private AudioSource m_activeMusicSource;
    private Coroutine m_activeCrossfadeRoutine;

    // --- SFX Source Pool ---
    private readonly List<AudioSource> m_sfxPool = new();
    private GameObject m_sfxPoolContainer;

    // --- Volume Cache ---
    private float m_masterVolume = 1f;
    private float m_musicVolume  = 1f;
    private float m_sfxVolume    = 1f;

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Cancel any lingering fade routines from scene transitions
        if (m_activeCrossfadeRoutine != null)
        {
            StopCoroutine(m_activeCrossfadeRoutine);
            m_activeCrossfadeRoutine = null;
        }

        // Re-apply saved volumes to mixer when scene is reloaded/restarted
        StartCoroutine(ApplyVolumesDelayed());
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (m_dontDestroyOnLoad && Application.isPlaying)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        InitializeAudioSources();
        LoadVolumes();
    }

    private void Start()
    {
        // Mixer parameters must be set after Awake (Unity AudioMixer initialization timing)
        StartCoroutine(ApplyVolumesDelayed());

        if (m_playOnStartMusic != null && (m_activeMusicSource == null || m_activeMusicSource.clip == null || !m_activeMusicSource.isPlaying))
        {
            PlayMusic(m_playOnStartMusic, m_startMusicFadeIn);
        }
    }

    private IEnumerator ApplyVolumesDelayed()
    {
        yield return new WaitForEndOfFrame();
        LoadVolumes();
    }

    // ──────────────────────────── Initialization ────────────────────────────

    private void InitializeAudioSources()
    {
        // Channel A (routed to Music mixer group)
        m_musicSourceA = CreateAudioSource("MusicChannel_A", m_musicGroup);
        m_musicSourceA.loop = true;

        // Channel B (routed to Music mixer group)
        m_musicSourceB = CreateAudioSource("MusicChannel_B", m_musicGroup);
        m_musicSourceB.loop = true;

        m_activeMusicSource = m_musicSourceA;

        // Container for pooled SFX
        m_sfxPoolContainer = new GameObject("SFX_Pool");
        m_sfxPoolContainer.transform.SetParent(transform);

        for (int i = 0; i < m_initialSfxPoolSize; i++)
        {
            m_sfxPool.Add(CreatePooledSfxSource());
        }
    }

    private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup group)
    {
        GameObject go = new(sourceName);
        go.transform.SetParent(transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D by default
        if (group != null)
            src.outputAudioMixerGroup = group;
        return src;
    }

    private AudioSource CreatePooledSfxSource()
    {
        GameObject go = new("SFX_Source");
        go.transform.SetParent(m_sfxPoolContainer.transform);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        if (m_sfxGroup != null)
            src.outputAudioMixerGroup = m_sfxGroup;
        return src;
    }

    // ──────────────────────────── Volumes & Mixer ────────────────────────────

    /// <summary>Synchronizes volume settings from GameSettings.</summary>
    public void LoadVolumes()
    {
        if (m_settings != null)
        {
            m_settings.EnsureLoaded();
            m_masterVolume = m_settings.AudioGeneral;
            m_musicVolume  = m_settings.AudioMusic;
            m_sfxVolume    = m_settings.AudioSFX;
        }
        else
        {
            m_masterVolume = 1f;
            m_musicVolume  = 1f;
            m_sfxVolume    = 1f;
        }

        ApplyVolumes();
    }

    /// <summary>Call whenever volume sliders in the UI are updated.</summary>
    public void UpdateVolumes(float master, float sfx, float music)
    {
        m_masterVolume = Mathf.Clamp01(master);
        m_sfxVolume    = Mathf.Clamp01(sfx);
        m_musicVolume  = Mathf.Clamp01(music);
        ApplyVolumes();
    }

    public void SetMasterVolume(float volume)
    {
        m_masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        m_sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        m_musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (m_audioMixer != null)
        {
            SetMixerDecibels(m_masterParamName, m_masterVolume, m_maxMasterDb);
            SetMixerDecibels(m_musicParamName,  m_musicVolume,  m_maxMusicDb);
            SetMixerDecibels(m_sfxParamName,    m_sfxVolume,    m_maxSfxDb);
        }
        else
        {
            // Fallback when no AudioMixer is assigned
            AudioListener.volume = m_masterVolume;

            if (m_activeCrossfadeRoutine == null && m_activeMusicSource != null && m_activeMusicSource.isPlaying)
            {
                float gain = Mathf.Pow(10f, m_maxMusicDb / 20f);
                m_activeMusicSource.volume = Mathf.Clamp01(m_musicVolume * gain);
            }
        }
    }

    private void SetMixerDecibels(string paramName, float linear, float maxDb)
    {
        if (string.IsNullOrEmpty(paramName) || m_audioMixer == null) return;
        float db = LinearToDecibels(linear, maxDb, m_minDb);
        m_audioMixer.SetFloat(paramName, db);
    }

    /// <summary>
    /// Converts a linear slider value (0.0 to 1.0) to decibels, scaled up to maxDb.
    /// With maxDb = 10f, slider at 1.0 produces +10dB, and slider at 0.0 produces -80dB (mute).
    /// </summary>
    public static float LinearToDecibels(float linear, float maxDb = 0f, float minDb = -80f)
    {
        if (linear <= 0.0001f)
            return minDb; // Complete silence

        float db = Mathf.Log10(linear) * 20f + maxDb;
        return Mathf.Clamp(db, minDb, maxDb);
    }

    // ──────────────────────────── Music Crossfade ────────────────────────────

    /// <summary>
    /// Smoothly transitions from the current music track to a new track via crossfade.
    /// If the same track is already playing, it will keep playing uninterrupted.
    /// </summary>
    public void PlayMusic(AudioClip clip, float fadeDuration = 1.5f, bool loop = true)
    {
        if (clip == null) return;

        // Avoid restarting if already playing the exact track with audible volume
        if (m_activeMusicSource != null && m_activeMusicSource.isPlaying && m_activeMusicSource.clip == clip && m_activeMusicSource.volume > 0.05f)
            return;

        // Ensure groups are assigned even if added dynamically in editor
        if (m_musicGroup != null)
        {
            m_musicSourceA.outputAudioMixerGroup = m_musicGroup;
            m_musicSourceB.outputAudioMixerGroup = m_musicGroup;
        }

        // Choose the alternate inactive channel for incoming track
        AudioSource incoming = (m_activeMusicSource == m_musicSourceA) ? m_musicSourceB : m_musicSourceA;
        AudioSource outgoing = m_activeMusicSource;

        incoming.clip = clip;
        incoming.loop = loop;
        incoming.volume = 0f;
        incoming.Play();

        m_activeMusicSource = incoming;

        if (m_activeCrossfadeRoutine != null)
            StopCoroutine(m_activeCrossfadeRoutine);

        m_activeCrossfadeRoutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming, fadeDuration));
    }

    /// <summary>Fades out the current music track to silence.</summary>
    public void FadeOutMusic(float fadeDuration = 1.5f)
    {
        if (m_activeMusicSource == null || !m_activeMusicSource.isPlaying) return;

        if (m_activeCrossfadeRoutine != null)
            StopCoroutine(m_activeCrossfadeRoutine);

        m_activeCrossfadeRoutine = StartCoroutine(FadeOutRoutine(m_activeMusicSource, fadeDuration));
    }

    public void StopMusic()
    {
        if (m_activeCrossfadeRoutine != null)
        {
            StopCoroutine(m_activeCrossfadeRoutine);
            m_activeCrossfadeRoutine = null;
        }

        if (m_musicSourceA != null) { m_musicSourceA.Stop(); m_musicSourceA.clip = null; }
        if (m_musicSourceB != null) { m_musicSourceB.Stop(); m_musicSourceB.clip = null; }
    }

    private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming, float duration)
    {
        float elapsed = 0f;
        float startOutVol = outgoing != null ? outgoing.volume : 0f;
        // When using AudioMixer, the AudioSource volume operates in 0 to 1 range
        float targetInVol = (m_audioMixer != null) ? 1f : m_musicVolume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (outgoing != null)
                outgoing.volume = Mathf.Lerp(startOutVol, 0f, t);

            if (incoming != null)
                incoming.volume = Mathf.Lerp(0f, targetInVol, t);

            yield return null;
        }

        if (outgoing != null)
        {
            outgoing.volume = 0f;
            outgoing.Stop();
            outgoing.clip = null;
        }

        if (incoming != null)
        {
            incoming.volume = targetInVol;
        }

        m_activeCrossfadeRoutine = null;
    }

    private IEnumerator FadeOutRoutine(AudioSource source, float duration)
    {
        float elapsed = 0f;
        float startVol = source.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        source.clip = null;
        m_activeCrossfadeRoutine = null;
    }

    // ──────────────────────────── SFX Playback ────────────────────────────

    /// <summary>Plays a 2D sound effect using a pooled source.</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetFreeSfxSource();
        src.transform.position = transform.position;
        src.spatialBlend = 0f; // 2D
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        // If mixer is active, volume is handled by mixer, else scaled by m_sfxVolume
        float baseVol = (m_audioMixer != null) ? 1f : m_sfxVolume;
        src.volume = baseVol * Mathf.Clamp01(volumeScale);
        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    /// <summary>Plays a 3D sound effect at a specific world position.</summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

        AudioSource src = GetFreeSfxSource();
        src.transform.position = position;
        src.spatialBlend = 1f; // 3D spatial
        src.minDistance = 2f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        float baseVol = (m_audioMixer != null) ? 1f : m_sfxVolume;
        src.volume = baseVol * Mathf.Clamp01(volumeScale);
        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    /// <summary>Plays a modular AudioCue data container.</summary>
    public void PlayAudioCue(AudioCue cue)
    {
        if (cue == null) return;
        AudioClip clip = cue.GetRandomClip();
        if (clip != null)
        {
            PlaySFX(clip, cue.Volume, cue.PitchVariance);
        }
    }

    /// <summary>Plays a modular AudioCue at a 3D world position.</summary>
    public void PlayAudioCueAt(AudioCue cue, Vector3 position)
    {
        if (cue == null) return;
        AudioClip clip = cue.GetRandomClip();
        if (clip != null)
        {
            PlaySFXAtPosition(clip, position, cue.Volume, cue.PitchVariance);
        }
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < m_sfxPool.Count; i++)
        {
            if (!m_sfxPool[i].isPlaying)
                return m_sfxPool[i];
        }

        // Expand pool if all are currently busy
        AudioSource newSource = CreatePooledSfxSource();
        m_sfxPool.Add(newSource);
        return newSource;
    }
}
