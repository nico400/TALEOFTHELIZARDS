using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Modular UI sound feedback component.
/// Attach to any UI Button or Selectable to automatically play sounds on navigation/hover and click/submit.
/// Single Responsibility: UI interaction audio triggering.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class ButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerClickHandler, ISubmitHandler
{
    [Header("Audio Clips")]
    [Tooltip("Sound played when highlighted with mouse or selected via controller/keyboard.")]
    [SerializeField] private AudioClip m_selectSound;

    [Tooltip("Sound played when button is clicked or submitted via controller/keyboard.")]
    [SerializeField] private AudioClip m_clickSound;

    [Header("Audio Cue Alternative (Optional)")]
    [SerializeField] private AudioCue m_selectCue;
    [SerializeField] private AudioCue m_clickCue;

    [Header("Fine Tuning")]
    [SerializeField, Range(0f, 1f)] private float m_volumeScale = 1f;

    private Selectable m_selectable;

    private void Awake()
    {
        m_selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData) => PlaySelectSound();
    public void OnSelect(BaseEventData eventData) => PlaySelectSound();

    public void OnPointerClick(PointerEventData eventData) => PlayClickSound();
    public void OnSubmit(BaseEventData eventData) => PlayClickSound();

    private void PlaySelectSound()
    {
        if (m_selectable != null && !m_selectable.interactable) return;
        if (AudioManager.Instance == null) return;

        if (m_selectCue != null)
        {
            AudioManager.Instance.PlayAudioCue(m_selectCue);
        }
        else if (m_selectSound != null)
        {
            AudioManager.Instance.PlaySFX(m_selectSound, m_volumeScale, pitchVariance: 0.03f);
        }
    }

    private void PlayClickSound()
    {
        if (m_selectable != null && !m_selectable.interactable) return;
        if (AudioManager.Instance == null) return;

        if (m_clickCue != null)
        {
            AudioManager.Instance.PlayAudioCue(m_clickCue);
        }
        else if (m_clickSound != null)
        {
            AudioManager.Instance.PlaySFX(m_clickSound, m_volumeScale, pitchVariance: 0.02f);
        }
    }
}

