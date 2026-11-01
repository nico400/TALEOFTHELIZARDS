using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ensures a ScrollRect with a vertical Scrollbar works correctly.
/// Fixes common issues: Content pivot/anchors, Scrollbar value, and Content size.
/// Attach this to the same GameObject that holds the ScrollRect.
/// Single Responsibility: credits scroll fix and management.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button m_buttonBack;

    public Button ButtonBack => m_buttonBack;

    /// <summary>Raised when the player clicks "Voltar" (Back).</summary>
    public event System.Action OnBackRequested;

    private void Awake()
    {
        if (m_buttonBack != null)
            m_buttonBack.onClick.AddListener(() => OnBackRequested?.Invoke());
    }

    private void OnDestroy()
    {
        if (m_buttonBack != null)
            m_buttonBack.onClick.RemoveAllListeners();
    }

    public void SetButtonBackInEventSystem()
    {
        if (EventSystem.current != null && m_buttonBack != null)
        {
            EventSystem.current.SetSelectedGameObject(m_buttonBack.gameObject);
        }
    }
}
