using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main Menu orchestrator.
/// Manages panel visibility, button wiring, gamepad validation pop-up,
/// exit confirmation, dynamic controller focus navigation, and scene transitions with fade.
/// 
/// Architecture notes (SOLID):
///   - SRP: Each panel/feature has its own script (SettingsPanel, CreditsPanel,
///          GamepadDetector, ScreenFader). MenuManager only orchestrates.
///   - OCP: New panels can be added without modifying existing panel scripts.
///   - DIP: Depends on abstractions (events) rather than concrete implementations
///          for audio (hook your AudioManager via events/GameSettings).
/// </summary>
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
    [SerializeField] private Image m_iconControllerObj;    // Icon shown when gamepad is connected
    [SerializeField] private Sprite m_iconControllerActive;
    [SerializeField] private Sprite m_iconControllerInactive;

    [Header("Transition")]
    [SerializeField] private ScreenFader m_screenFader;
    [SerializeField] private string m_gameplaySceneName = "VeterinarianLevel";

    [Header("Audio")]
    [Tooltip("Optional: Menu music clip to trigger through AudioManager on start.")]
    [SerializeField] private AudioClip m_menuMusicClip;
    [Tooltip("Optional fallback: Direct AudioSource playing the menu music. Used for fade-out on Play.")]
    [SerializeField] private AudioSource m_menuMusic;
    [SerializeField, Min(0.1f)] private float m_musicFadeDuration = 1.5f;

    // ──────────────────────────── Cached References ────────────────────────────

    private SettingsPanel m_settingsPanelScript;
    private CreditsPanel m_creditsScrollerScript;
    private bool m_isTransitioning;
    private Coroutine m_selectRoutine;

    // ──────────────────────────── Unity Lifecycle ────────────────────────────

    private void Awake()
    {
        // Cache panel scripts.
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

        // Start menu music via AudioManager if assigned
        if (m_menuMusicClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(m_menuMusicClip, m_musicFadeDuration);
        }

        // Focus first button if gamepad is already present
        if (m_gamepadDetector != null && m_gamepadDetector.IsGamepadConnected)
        {
            SelectUIElement(GetActivePanelDefaultSelectable());
        }
    }

    private void Update()
    {
        // If gamepad is connected and selection is lost (e.g. mouse clicked background or pop-up just closed/opened),
        // recover focus whenever the user touches navigation input on gamepad or keyboard.
        if (m_isTransitioning) return;

        bool hasGamepad = m_gamepadDetector != null && m_gamepadDetector.IsGamepadConnected;
        if (!hasGamepad) return;

        // Don't auto-select if warning panel is currently showing (gamepad blocked)
        if (m_panelWarning != null && m_panelWarning.activeSelf) return;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
        {
            if (HasNavigationInput())
            {
                SelectUIElement(GetActivePanelDefaultSelectable());
            }
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
        if (m_iconControllerObj != null)
            if (connected) { 
                m_iconControllerObj.sprite = m_iconControllerActive;
            }
            else {
                m_iconControllerObj.sprite = m_iconControllerInactive;
            }

        // Block or release menu navigation.
        SetMainButtonsInteractable(!showWarning);

        if (connected)
        {
            // Reconnected! Immediately re-focus the active panel's button.
            SelectUIElement(GetActivePanelDefaultSelectable());
        }
        else
        {
            // Disconnected: clear selection so broken navigation states don't linger.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
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

        // Fade out music through AudioManager (or fallback AudioSource)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeOutMusic(m_musicFadeDuration);
        }
        else if (m_menuMusic != null)
        {
            StartCoroutine(FadeMusicOut(m_menuMusic, m_musicFadeDuration));
        }

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

        GameObject target = m_settingsPanelScript != null ? m_settingsPanelScript.FirstSelectable : null;
        if (target == null && m_settingsPanelScript != null && m_settingsPanelScript.ButtonBack != null)
            target = m_settingsPanelScript.ButtonBack.gameObject;

        SelectUIElement(target);
    }

    private void OnSettingsBack()
    {
        if (m_settingsPanelScript != null)
            m_settingsPanelScript.Close();
        else if (m_panelSettings != null)
            m_panelSettings.SetActive(false);

        // Return focus to the Settings button in the main menu
        SelectUIElement(m_buttonSettings != null ? m_buttonSettings.gameObject : m_buttonPlay.gameObject);
    }

    private void OnControllerMappingClicked()
    {
        CloseAllPopups();

        if (m_panelControllerMapping != null)
            m_panelControllerMapping.SetActive(true);

        SelectUIElement(m_buttonBackMapping != null ? m_buttonBackMapping.gameObject : null);
    }

    private void OnControllerMappingBack()
    {
        if (m_panelControllerMapping != null)
            m_panelControllerMapping.SetActive(false);

        // Return focus to the Controller Mapping button in the main menu
        SelectUIElement(m_buttonControllerMapping != null ? m_buttonControllerMapping.gameObject : m_buttonPlay.gameObject);
    }

    private void OnCreditsClicked()
    {
        CloseAllPopups();

        if (m_panelCredits != null)
            m_panelCredits.SetActive(true);

        GameObject target = null;
        if (m_creditsScrollerScript != null && m_creditsScrollerScript.ButtonBack != null)
            target = m_creditsScrollerScript.ButtonBack.gameObject;

        SelectUIElement(target);
    }

    private void OnCreditsBack()
    {
        if (m_panelCredits != null)
            m_panelCredits.SetActive(false);

        // Return focus to Credits button in the main menu
        SelectUIElement(m_buttonCredits != null ? m_buttonCredits.gameObject : m_buttonPlay.gameObject);
    }

    private void OnExitClicked()
    {
        CloseAllPopups();

        if (m_panelExit != null)
            m_panelExit.SetActive(true);

        // Standard UX practice: default selection on exit confirmation is 'NO'
        GameObject target = m_buttonNo != null ? m_buttonNo.gameObject : (m_buttonYes != null ? m_buttonYes.gameObject : null);
        SelectUIElement(target);
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

        // Return focus to Exit button in the main menu
        SelectUIElement(m_buttonExit != null ? m_buttonExit.gameObject : m_buttonPlay.gameObject);
    }

    // ──────────────────────────── Dynamic Selection Management ────────────────────────────

    /// <summary>
    /// Safely selects a UI GameObject on the next frame so uGUI has time to activate and enable components.
    /// </summary>
    public void SelectUIElement(GameObject target)
    {
        if (target == null) return;
        if (!gameObject.activeInHierarchy) return;

        if (m_selectRoutine != null)
            StopCoroutine(m_selectRoutine);

        m_selectRoutine = StartCoroutine(SelectUIRoutine(target));
    }

    private IEnumerator SelectUIRoutine(GameObject target)
    {
        // Clear selection first so uGUI forces a fresh highlight transition
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        yield return null; // Wait 1 frame for hierarchy and active states to settle

        if (EventSystem.current != null && target != null && target.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }

        m_selectRoutine = null;
    }

    /// <summary>
    /// Evaluates which pop-up or screen is currently active and returns its default selectable element.
    /// </summary>
    public GameObject GetActivePanelDefaultSelectable()
    {
        if (m_panelWarning != null && m_panelWarning.activeSelf)
            return null;

        if (m_panelExit != null && m_panelExit.activeSelf)
            return m_buttonNo != null ? m_buttonNo.gameObject : (m_buttonYes != null ? m_buttonYes.gameObject : null);

        if (m_panelSettings != null && m_panelSettings.activeSelf)
        {
            if (m_settingsPanelScript != null && m_settingsPanelScript.FirstSelectable != null)
                return m_settingsPanelScript.FirstSelectable;
            if (m_settingsPanelScript != null && m_settingsPanelScript.ButtonBack != null)
                return m_settingsPanelScript.ButtonBack.gameObject;
        }

        if (m_panelControllerMapping != null && m_panelControllerMapping.activeSelf)
            return m_buttonBackMapping != null ? m_buttonBackMapping.gameObject : null;

        if (m_panelCredits != null && m_panelCredits.activeSelf)
            return m_creditsScrollerScript != null && m_creditsScrollerScript.ButtonBack != null
                ? m_creditsScrollerScript.ButtonBack.gameObject
                : null;

        // Default to main menu
        return m_buttonPlay != null ? m_buttonPlay.gameObject : null;
    }

    /// <summary>
    /// Checks whether user pressed directional inputs on Gamepad or Keyboard.
    /// </summary>
    private bool HasNavigationInput()
    {
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            if (stick.sqrMagnitude > 0.2f || dpad.sqrMagnitude > 0.2f)
                return true;

            if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.buttonEast.wasPressedThisFrame)
                return true;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame ||
                Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    // ──────────────────────────── Utilities ────────────────────────────

    private void CloseAllPopups()
    {
        if (m_panelSettings != null) m_panelSettings.SetActive(false);

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
