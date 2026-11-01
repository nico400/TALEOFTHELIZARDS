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

    // --- Runtime values (loaded from PlayerPrefs) ---
    public float SensMouse   { get; set; }
    public float SensGamepad { get; set; }
    public float AudioGeneral { get; set; }
    public float AudioSFX     { get; set; }
    public float AudioMusic   { get; set; }

    /// <summary>Call once on game start to load saved values.</summary>
    public void Load()
    {
        EditorLogger.Log("Ele entrou no Load", Color.green);
        EditorLogger.Log("SensMouse: " + SensMouse, Color.green);
        EditorLogger.Log("SensGamepad: " + SensGamepad, Color.green);
        SensMouse    = PlayerPrefs.GetFloat(KEY_SENS_MOUSE,    m_defaultSensMouse);
        SensGamepad  = PlayerPrefs.GetFloat(KEY_SENS_GAMEPAD,  m_defaultSensGamepad);
        AudioGeneral = PlayerPrefs.GetFloat(KEY_AUDIO_GENERAL, m_defaultAudioGeneral);
        AudioSFX     = PlayerPrefs.GetFloat(KEY_AUDIO_SFX,     m_defaultAudioSFX);
        AudioMusic   = PlayerPrefs.GetFloat(KEY_AUDIO_MUSIC,   m_defaultAudioMusic);
    }

    /// <summary>Persist current values to disk.</summary>
    public void Save()
    {
        PlayerPrefs.SetFloat(KEY_SENS_MOUSE,    SensMouse);
        PlayerPrefs.SetFloat(KEY_SENS_GAMEPAD,  SensGamepad);
        PlayerPrefs.SetFloat(KEY_AUDIO_GENERAL, AudioGeneral);
        PlayerPrefs.SetFloat(KEY_AUDIO_SFX,     AudioSFX);
        PlayerPrefs.SetFloat(KEY_AUDIO_MUSIC,   AudioMusic);
        PlayerPrefs.Save();
    }

    /// <summary>Reset all values to their inspector defaults.</summary>
    public void ResetToDefaults()
    {
        SensMouse    = m_defaultSensMouse;
        SensGamepad  = m_defaultSensGamepad;
        AudioGeneral = m_defaultAudioGeneral;
        AudioSFX     = m_defaultAudioSFX;
        AudioMusic   = m_defaultAudioMusic;
    }
}

