using LFramework.Utility;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Settings pop-up panel, binding sliders to a <see cref="GameSettings"/> asset.
/// Single Responsibility: settings panel UI binding.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameSettings m_settings;

    [Header("Sliders")]
    [SerializeField] private Slider m_sliderSensMouse;
    [SerializeField] private Slider m_sliderSensGamepad;
    [SerializeField] private Slider m_sliderAudioGeneral;
    [SerializeField] private Slider m_sliderSFX;
    [SerializeField] private Slider m_sliderMusic;

    [Header("Navigation")]
    [SerializeField] private Button m_buttonBack;

    public Button ButtonBack => m_buttonBack;
    public GameObject FirstSelectable => m_sliderSensMouse != null ? m_sliderSensMouse.gameObject : (m_buttonBack != null ? m_buttonBack.gameObject : null);

    /// <summary>Raised when the player clicks "Voltar" (Back).</summary>
    public event Action OnBackRequested;

    private bool m_isInitialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        UnbindSliders();

        if (m_buttonBack != null)
            m_buttonBack.onClick.RemoveListener(HandleBack);
    }

    /// <summary>
    /// Guarantees that sliders are bounded, listeners are attached, and settings are loaded.
    /// Can be safely called before or after Awake.
    /// </summary>
    public void EnsureInitialized()
    {
        if (m_isInitialized) return;

        if (m_settings == null)
        {
            EditorLogger.LogError($"[{nameof(SettingsPanel)}] GameSettings asset not assigned!");
            return;
        }

        m_settings.EnsureLoaded();

        // Enforce slider min/max bounds so values don't clamp to 0 or 1 incorrectly
        ConfigureSliderBounds();

        SyncSlidersFromSettings();
        BindSliders();

        if (m_buttonBack != null)
        {
            m_buttonBack.onClick.RemoveListener(HandleBack);
            m_buttonBack.onClick.AddListener(HandleBack);
        }

        m_isInitialized = true;
    }

    /// <summary>Call when opening the panel to refresh slider positions.</summary>
    public void Open()
    {
        gameObject.SetActive(true);
        EnsureInitialized();
        SyncSlidersFromSettings();
    }

    /// <summary>Hide the panel and save settings.</summary>
    public void Close()
    {
        if (m_settings != null && m_isInitialized)
        {
            m_settings.Save();
        }
        gameObject.SetActive(false);
    }

    // -------- Internals --------

    private void ConfigureSliderBounds()
    {
        if (m_sliderSensMouse != null)
        {
            m_sliderSensMouse.minValue = 2f;
            m_sliderSensMouse.maxValue = 20f;
        }

        if (m_sliderSensGamepad != null)
        {
            m_sliderSensGamepad.minValue = 50f;
            m_sliderSensGamepad.maxValue = 400f;
        }

        if (m_sliderAudioGeneral != null)
        {
            m_sliderAudioGeneral.minValue = 0f;
            m_sliderAudioGeneral.maxValue = 1f;
        }

        if (m_sliderSFX != null)
        {
            m_sliderSFX.minValue = 0f;
            m_sliderSFX.maxValue = 1f;
        }

        if (m_sliderMusic != null)
        {
            m_sliderMusic.minValue = 0f;
            m_sliderMusic.maxValue = 1f;
        }
    }

    private void SyncSlidersFromSettings()
    {
        if (m_settings == null) return;
        m_settings.EnsureLoaded();

        if (m_sliderSensMouse    != null) m_sliderSensMouse.SetValueWithoutNotify(m_settings.SensMouse);
        if (m_sliderSensGamepad  != null) m_sliderSensGamepad.SetValueWithoutNotify(m_settings.SensGamepad);
        if (m_sliderAudioGeneral != null) m_sliderAudioGeneral.SetValueWithoutNotify(m_settings.AudioGeneral);
        if (m_sliderSFX          != null) m_sliderSFX.SetValueWithoutNotify(m_settings.AudioSFX);
        if (m_sliderMusic        != null) m_sliderMusic.SetValueWithoutNotify(m_settings.AudioMusic);
    }

    private void BindSliders()
    {
        UnbindSliders();

        if (m_sliderSensMouse    != null) m_sliderSensMouse.onValueChanged.AddListener(v    => m_settings.SensMouse    = v);
        if (m_sliderSensGamepad  != null) m_sliderSensGamepad.onValueChanged.AddListener(v  => m_settings.SensGamepad  = v);
        if (m_sliderAudioGeneral != null) m_sliderAudioGeneral.onValueChanged.AddListener(v => { m_settings.AudioGeneral = v; ApplyAudio(); });
        if (m_sliderSFX          != null) m_sliderSFX.onValueChanged.AddListener(v          => { m_settings.AudioSFX     = v; ApplyAudio(); });
        if (m_sliderMusic        != null) m_sliderMusic.onValueChanged.AddListener(v        => { m_settings.AudioMusic   = v; ApplyAudio(); });
    }

    private void UnbindSliders()
    {
        if (m_sliderSensMouse    != null) m_sliderSensMouse.onValueChanged.RemoveAllListeners();
        if (m_sliderSensGamepad  != null) m_sliderSensGamepad.onValueChanged.RemoveAllListeners();
        if (m_sliderAudioGeneral != null) m_sliderAudioGeneral.onValueChanged.RemoveAllListeners();
        if (m_sliderSFX          != null) m_sliderSFX.onValueChanged.RemoveAllListeners();
        if (m_sliderMusic        != null) m_sliderMusic.onValueChanged.RemoveAllListeners();
    }

    /// <summary>
    /// Applies audio volume changes in real-time.
    /// Hook your AudioMixer or AudioManager here when ready.
    /// </summary>
    private void ApplyAudio()
    {
        AudioListener.volume = m_settings.AudioGeneral;
    }

    private void HandleBack()
    {
        Close();
        OnBackRequested?.Invoke();
    }
}
