using LFramework.Utility;
using UnityEngine;

/// <summary>
/// Persistent ScriptableObject that stores player settings (sensitivity, audio volumes).
/// Survives scene transitions and can be shared across systems.
/// Single Responsibility: data storage for user preferences.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameSettings", order = 2)]
public class GameSettings : ScriptableObject
{
    private const string KEY_SENS_MOUSE       = "Settings_SensMouse";
    private const string KEY_SENS_GAMEPAD     = "Settings_SensGamepad";
    private const string KEY_AUDIO_GENERAL    = "Settings_AudioGeneral";
    private const string KEY_AUDIO_SFX        = "Settings_AudioSFX";
    private const string KEY_AUDIO_MUSIC      = "Settings_AudioMusic";

    [Header("Sensitivity Defaults")]
    [SerializeField, Range(2f, 20f)] private float m_defaultSensMouse   = 9f;
    [SerializeField, Range(100f, 400f)] private float m_defaultSensGamepad = 150f;

    [Header("Audio Defaults (0-1)")]
    [SerializeField, Range(0f, 1f)] private float m_defaultAudioGeneral = 1f;
    [SerializeField, Range(0f, 1f)] private float m_defaultAudioSFX     = 1f;
    [SerializeField, Range(0f, 1f)] private float m_defaultAudioMusic   = 1f;

    public float DefaultSensMouse => m_defaultSensMouse;
    public float DefaultSensGamepad => m_defaultSensGamepad;
    public float DefaultAudioGeneral => m_defaultAudioGeneral;
    public float DefaultAudioSFX => m_defaultAudioSFX;
    public float DefaultAudioMusic => m_defaultAudioMusic;

    // --- Runtime values (loaded from PlayerPrefs) ---
    private float m_sensMouse;
    private float m_sensGamepad;
    private float m_audioGeneral;
    private float m_audioSFX;
    private float m_audioMusic;
    private bool m_isLoaded;

    public float SensMouse
    {
        get { EnsureLoaded(); return m_sensMouse; }
        set { EnsureLoaded(); m_sensMouse = value; }
    }

    public float SensGamepad
    {
        get { EnsureLoaded(); return m_sensGamepad; }
        set { EnsureLoaded(); m_sensGamepad = value; }
    }

    public float AudioGeneral
    {
        get { EnsureLoaded(); return m_audioGeneral; }
        set { EnsureLoaded(); m_audioGeneral = value; }
    }

    public float AudioSFX
    {
        get { EnsureLoaded(); return m_audioSFX; }
        set { EnsureLoaded(); m_audioSFX = value; }
    }

    public float AudioMusic
    {
        get { EnsureLoaded(); return m_audioMusic; }
        set { EnsureLoaded(); m_audioMusic = value; }
    }

    private void OnEnable()
    {
        m_isLoaded = false;
        Load();
    }

    /// <summary>Guarantees settings are loaded before reading or writing.</summary>
    public void EnsureLoaded()
    {
        if (!m_isLoaded)
        {
            Load();
        }
    }

    /// <summary>Call to load saved values from PlayerPrefs.</summary>
    public void Load()
    {
        m_sensMouse    = PlayerPrefs.GetFloat(KEY_SENS_MOUSE,    m_defaultSensMouse);
        m_sensGamepad  = PlayerPrefs.GetFloat(KEY_SENS_GAMEPAD,  m_defaultSensGamepad);
        m_audioGeneral = PlayerPrefs.GetFloat(KEY_AUDIO_GENERAL, m_defaultAudioGeneral);
        m_audioSFX     = PlayerPrefs.GetFloat(KEY_AUDIO_SFX,     m_defaultAudioSFX);
        m_audioMusic   = PlayerPrefs.GetFloat(KEY_AUDIO_MUSIC,   m_defaultAudioMusic);

        // Sanity recovery: sensitivity can never be 0 (recovers if 0 was accidentally saved previously)
        if (m_sensMouse <= 0.001f)
            m_sensMouse = m_defaultSensMouse;

        if (m_sensGamepad <= 0.001f)
            m_sensGamepad = m_defaultSensGamepad;

        m_isLoaded = true;
        EditorLogger.Log($"[GameSettings] Loaded: Mouse={m_sensMouse}, Gamepad={m_sensGamepad}, Master={m_audioGeneral}, SFX={m_audioSFX}, Music={m_audioMusic}", Color.green);
    }

    /// <summary>Persist current values to disk.</summary>
    public void Save()
    {
        EnsureLoaded();

        // Never save 0 for sensitivity
        if (m_sensMouse <= 0.001f) m_sensMouse = m_defaultSensMouse;
        if (m_sensGamepad <= 0.001f) m_sensGamepad = m_defaultSensGamepad;

        PlayerPrefs.SetFloat(KEY_SENS_MOUSE,    m_sensMouse);
        PlayerPrefs.SetFloat(KEY_SENS_GAMEPAD,  m_sensGamepad);
        PlayerPrefs.SetFloat(KEY_AUDIO_GENERAL, m_audioGeneral);
        PlayerPrefs.SetFloat(KEY_AUDIO_SFX,     m_audioSFX);
        PlayerPrefs.SetFloat(KEY_AUDIO_MUSIC,   m_audioMusic);
        PlayerPrefs.Save();

        EditorLogger.Log($"[GameSettings] Saved: Mouse={m_sensMouse}, Gamepad={m_sensGamepad}", Color.cyan);
    }

    /// <summary>Reset all values to their inspector defaults.</summary>
    public void ResetToDefaults()
    {
        m_sensMouse    = m_defaultSensMouse;
        m_sensGamepad  = m_defaultSensGamepad;
        m_audioGeneral = m_defaultAudioGeneral;
        m_audioSFX     = m_defaultAudioSFX;
        m_audioMusic   = m_defaultAudioMusic;
        m_isLoaded     = true;
    }
}
