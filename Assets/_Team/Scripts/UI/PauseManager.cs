using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// In-game Pause Manager that displays the Settings popup directly on pause,
/// allowing live sensitivity and audio adjustments during gameplay.
/// Single Responsibility: managing pause state and pause menu visibility.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Root GameObject of the Pause/Settings popup panel in the scene Canvas.")]
    [SerializeField] private GameObject m_pausePanel;

    [Tooltip("SettingsPanel component handling the sliders.")]
    [SerializeField] private SettingsPanel m_settingsPanel;

    [Header("Audio Feedback (Optional)")]
    [SerializeField] private AudioClip m_pauseSound;
    [SerializeField] private AudioClip m_resumeSound;

    [Header("Options")]
    [Tooltip("Whether to lock and hide the mouse cursor when resuming gameplay.")]
    [SerializeField] private bool m_lockCursorOnResume = true;

    /// <summary>Fires whenever pause state changes (true = paused, false = resumed).</summary>
    public event Action<bool> OnPauseToggled;

    public bool IsPaused { get; private set; }

    private Coroutine m_selectRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (m_settingsPanel == null && m_pausePanel != null)
        {
            m_settingsPanel = m_pausePanel.GetComponentInChildren<SettingsPanel>(true);
        }

        if (m_settingsPanel != null)
        {
            m_settingsPanel.OnBackRequested += Resume;
        }

        // Ensure pause panel starts closed
        if (m_pausePanel != null)
        {
            m_pausePanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_settingsPanel != null)
        {
            m_settingsPanel.OnBackRequested -= Resume;
        }

        // Guarantee timeScale is restored if scene is unloaded while paused
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (WasPausePressed())
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;

        // Unlock mouse cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Play sound if assigned
        if (m_pauseSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(m_pauseSound);
        }

        // Open settings panel
        if (m_settingsPanel != null)
        {
            m_settingsPanel.Open();
        }
        else if (m_pausePanel != null)
        {
            m_pausePanel.SetActive(true);
        }

        // Focus first control for Gamepad navigation
        FocusFirstElement();

        OnPauseToggled?.Invoke(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;

        // Play sound if assigned
        if (m_resumeSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(m_resumeSound);
        }

        // Close and save settings
        if (m_settingsPanel != null)
        {
            m_settingsPanel.Close();
        }
        else if (m_pausePanel != null)
        {
            m_pausePanel.SetActive(false);
        }

        // Re-lock mouse cursor for gameplay
        if (m_lockCursorOnResume)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        OnPauseToggled?.Invoke(false);
    }

    private void FocusFirstElement()
    {
        GameObject target = null;
        if (m_settingsPanel != null)
        {
            target = m_settingsPanel.FirstSelectable;
        }

        if (target != null)
        {
            if (m_selectRoutine != null) StopCoroutine(m_selectRoutine);
            m_selectRoutine = StartCoroutine(SelectElementRoutine(target));
        }
    }

    private IEnumerator SelectElementRoutine(GameObject target)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        yield return null; // Wait 1 unscaled frame for uGUI hierarchy to activate

        if (EventSystem.current != null && target != null && target.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }

        m_selectRoutine = null;
    }

    private static bool WasPausePressed()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.pKey.wasPressedThisFrame)
                return true;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }
}

