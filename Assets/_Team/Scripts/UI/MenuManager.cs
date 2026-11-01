using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu orchestrator.
/// Manages panel visibility, button wiring, gamepad validation pop-up,
/// exit confirmation, and scene transitions with fade.
/// 
/// Architecture notes (SOLID):
///   - SRP: Each panel/feature has its own script (SettingsPanel, CreditsScroller,
///          GamepadDetector, ScreenFader). MenuManager only orchestrates.
///   - OCP: New panels can be added without modifying existing panel scripts.
///   - DIP: Depends on abstractions (events) rather than concrete implementations
///          for audio (hook your AudioManager via events/GameSettings).
/// </summary>
/// 
public class MenuManager : MonoBehaviour
{
    // ──────────────────────────── Inspector Fields ────────────────────────────

    [Header("Buttons — Main Menu")]
    [SerializeField] private Button m_buttonPlay;
    [SerializeField] private Button m_buttonSettings;
    [SerializeField] private Button m_buttonControllerMapping;
    [SerializeField] private Button m_buttonCredits;
    [SerializeField] private Button m_buttonExit;

    [Header("Panels — Pop-ups  (set the root GameObjects)")]
    [SerializeField] private GameObject m_panelSettings;           // Panel_PopUpSettings (com SettingsPanel)
    [SerializeField] private GameObject m_panelControllerMapping;  // Panel_PopUpControllerMapping
    [SerializeField] private GameObject m_panelCredits;            // Panel_PopUpCredits
    [SerializeField] private GameObject m_panelExit;               // Panel_PopUpExit
    [SerializeField] private GameObject m_panelWarning;            // Panel_PopUpWarning (gamepad not detected)

    [Header("Exit Confirmation")]
    [SerializeField] private Button m_buttonYes;
    [SerializeField] private Button m_buttonNo;

    [Header("Controller Mapping — Back")]
    [SerializeField] private Button m_buttonBackMapping;

    [Header("Gamepad Detection")]
    [SerializeField] private GamepadDetector m_gamepadDetector;
    [SerializeField] private GameObject m_iconControllerActive;    // Icon shown when gamepad is connected

    [Header("Transition")]
    [SerializeField] private ScreenFader m_screenFader;
    [SerializeField] private string m_gameplaySceneName = "VeterinarianLevel";

    [Header("Audio (modular — assign later)")]
    [Tooltip("Optional: AudioSource playing the menu music. Used for fade-out on Play.")]
    [SerializeField] private AudioSource m_menuMusic;
    [SerializeField, Min(0.1f)] private float m_musicFadeDuration = 1.5f;

    // ──────────────────────────── Cached References ────────────────────────────

    private SettingsPanel m_settingsPanelScript;
    private CreditsPanel m_creditsScrollerScript;
    private bool m_isTransitioning;

    // ──────────────────────────── Unity Lifecycle ────────────────────────────

    private void Awake()
    {
        // Cache panel scripts (added by you on the same GameObjects in the scene).
        if (m_panelSettings != null)
            m_settingsPanelScript = m_panelSettings.GetComponentInChildren<SettingsPanel>(true);

        if (m_panelCredits != null)
            m_creditsScrollerScript = m_panelCredits.GetComponentInChildren<CreditsPanel>(true);

        // Ensure cursor is visible and unlocked in the menu.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        WireButtons();
        WireSubPanelEvents();
        WireGamepadDetector();
        CloseAllPopups();

        // Start with a fade-in (screen starts black, fades to transparent).
        if (m_screenFader != null)
        {
            m_screenFader.SetOpaque();
            m_screenFader.FadeIn();
        }
    }

    private void OnDestroy()
    {
        UnwireButtons();
        UnwireSubPanelEvents();
        UnwireGamepadDetector();
    }

    // ──────────────────────────── Button Wiring ────────────────────────────

    private void WireButtons()
    {
        if (m_buttonPlay != null)              m_buttonPlay.onClick.AddListener(OnPlayClicked);
        if (m_buttonSettings != null)          m_buttonSettings.onClick.AddListener(OnSettingsClicked);
        if (m_buttonControllerMapping != null)  m_buttonControllerMapping.onClick.AddListener(OnControllerMappingClicked);
        if (m_buttonCredits != null)           m_buttonCredits.onClick.AddListener(OnCreditsClicked);
        if (m_buttonExit != null)              m_buttonExit.onClick.AddListener(OnExitClicked);

        if (m_buttonYes != null)               m_buttonYes.onClick.AddListener(OnExitConfirmed);
        if (m_buttonNo != null)                m_buttonNo.onClick.AddListener(OnExitCanceled);

        if (m_buttonBackMapping != null)       m_buttonBackMapping.onClick.AddListener(OnControllerMappingBack);
    }

    private void UnwireButtons()
    {
        if (m_buttonPlay != null)              m_buttonPlay.onClick.RemoveListener(OnPlayClicked);
        if (m_buttonSettings != null)          m_buttonSettings.onClick.RemoveListener(OnSettingsClicked);
        if (m_buttonControllerMapping != null)  m_buttonControllerMapping.onClick.RemoveListener(OnControllerMappingClicked);
        if (m_buttonCredits != null)           m_buttonCredits.onClick.RemoveListener(OnCreditsClicked);
        if (m_buttonExit != null)              m_buttonExit.onClick.RemoveListener(OnExitClicked);

        if (m_buttonYes != null)               m_buttonYes.onClick.RemoveListener(OnExitConfirmed);
        if (m_buttonNo != null)                m_buttonNo.onClick.RemoveListener(OnExitCanceled);

        if (m_buttonBackMapping != null)       m_buttonBackMapping.onClick.RemoveListener(OnControllerMappingBack);
    }

    // ──────────────────────────── Sub-Panel Events ────────────────────────────

    private void WireSubPanelEvents()
    {
        if (m_settingsPanelScript != null)
            m_settingsPanelScript.OnBackRequested += OnSettingsBack;

        if (m_creditsScrollerScript != null)
            m_creditsScrollerScript.OnBackRequested += OnCreditsBack;
    }

    private void UnwireSubPanelEvents()
    {
        if (m_settingsPanelScript != null)
            m_settingsPanelScript.OnBackRequested -= OnSettingsBack;

        if (m_creditsScrollerScript != null)
            m_creditsScrollerScript.OnBackRequested -= OnCreditsBack;
    }

    // ──────────────────────────── Gamepad Detector ────────────────────────────

    private void WireGamepadDetector()
    {
        if (m_gamepadDetector == null) return;

        m_gamepadDetector.OnGamepadStateChanged += HandleGamepadState;

        // Initial check.
        m_gamepadDetector.Refresh();
        HandleGamepadState(m_gamepadDetector.IsGamepadConnected);
    }

    private void UnwireGamepadDetector()
    {
        if (m_gamepadDetector != null)
            m_gamepadDetector.OnGamepadStateChanged -= HandleGamepadState;
    }

    private void HandleGamepadState(bool connected)
    {
        bool showWarning = !connected;

        if (m_panelWarning != null)
            m_panelWarning.SetActive(showWarning);

        // Update icon.
        if (m_iconControllerActive != null)
            m_iconControllerActive.SetActive(connected);

        // Block or release menu navigation.
        SetMainButtonsInteractable(!showWarning);
    }

    private void SetMainButtonsInteractable(bool interactable)
    {
        if (m_buttonPlay != null)              m_buttonPlay.interactable = interactable;
        if (m_buttonSettings != null)          m_buttonSettings.interactable = interactable;
        if (m_buttonControllerMapping != null)  m_buttonControllerMapping.interactable = interactable;
        if (m_buttonCredits != null)           m_buttonCredits.interactable = interactable;
        if (m_buttonExit != null)              m_buttonExit.interactable = interactable;
    }

    // ──────────────────────────── Button Handlers ────────────────────────────

    private void OnPlayClicked()
    {
        if (m_isTransitioning) return;
        m_isTransitioning = true;

        // Fade out music (if assigned).
        if (m_menuMusic != null)
            StartCoroutine(FadeMusicOut(m_menuMusic, m_musicFadeDuration));

        // Fade screen to black, then load gameplay scene.
        if (m_screenFader != null)
        {
            m_screenFader.FadeOut(onComplete: LoadGameplayScene, duration: m_musicFadeDuration);
        }
        else
        {
            LoadGameplayScene();
        }
    }

    private void LoadGameplayScene()
    {
        SceneManager.LoadScene(m_gameplaySceneName);
    }

    private void OnSettingsClicked()
    {
        CloseAllPopups();

        if (m_settingsPanelScript != null)
            m_settingsPanelScript.Open();
        else if (m_panelSettings != null)
            m_panelSettings.SetActive(true);
    }

    private void OnSettingsBack()
    {
        if (m_panelSettings != null)
            m_panelSettings.SetActive(false);
    }

    private void OnControllerMappingClicked()
    {
        CloseAllPopups();

        if (m_panelControllerMapping != null)
            m_panelControllerMapping.SetActive(true);
    }

    private void OnControllerMappingBack()
    {
        if (m_panelControllerMapping != null)
            m_panelControllerMapping.SetActive(false);
    }

    private void OnCreditsClicked()
    {
        CloseAllPopups();

        if (m_panelCredits != null)
            m_panelCredits.SetActive(true);
    }

    private void OnCreditsBack()
    {
        if (m_panelCredits != null)
            m_panelCredits.SetActive(false);
    }

    private void OnExitClicked()
    {
        CloseAllPopups();

        if (m_panelExit != null)
            m_panelExit.SetActive(true);
    }

    private void OnExitConfirmed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnExitCanceled()
    {
        if (m_panelExit != null)
            m_panelExit.SetActive(false);
    }

    // ──────────────────────────── Utilities ────────────────────────────

    private void CloseAllPopups()
    {
        // Use the scripts' Close() when available (so they can save state),
        // otherwise just deactivate.
        if (m_settingsPanelScript != null)
            m_settingsPanelScript.Close();
        else if (m_panelSettings != null)
            m_panelSettings.SetActive(false);

        if (m_panelControllerMapping != null) m_panelControllerMapping.SetActive(false);

        if (m_panelCredits != null) m_panelCredits.SetActive(false);

        if (m_panelExit != null) m_panelExit.SetActive(false);

        // Warning panel is controlled exclusively by GamepadDetector — don't close it here.
    }

    /// <summary>
    /// Coroutine that smoothly fades an AudioSource volume to 0.
    /// Modular: works with any AudioSource. When you add an AudioManager,
    /// replace this with a call to that system.
    /// </summary>
    private static IEnumerator FadeMusicOut(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying) yield break;

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }
}
