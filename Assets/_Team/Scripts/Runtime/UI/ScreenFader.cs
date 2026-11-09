using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles full-screen fade transitions using a UI Image overlay.
/// Single Responsibility: screen fading only.
/// </summary>
[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float m_defaultDuration = 1f;

    private Image m_overlay;
    private Coroutine m_activeRoutine;

    /// <summary>True while a fade is in progress.</summary>
    public bool IsFading => m_activeRoutine != null;

    private void Awake()
    {
        m_overlay = GetComponent<Image>();

        // Start fully transparent so the menu is visible.
        SetAlpha(0f);
        m_overlay.raycastTarget = false;
    }

    /// <summary>Fades the screen from transparent to opaque (black).</summary>
    public void FadeOut(Action onComplete = null, float? duration = null)
    {
        StartFade(0f, 1f, duration ?? m_defaultDuration, onComplete);
    }

    /// <summary>Fades the screen from opaque (black) to transparent.</summary>
    public void FadeIn(Action onComplete = null, float? duration = null)
    {
        StartFade(1f, 0f, duration ?? m_defaultDuration, onComplete);
    }

    /// <summary>Immediately sets the overlay to fully opaque.</summary>
    public void SetOpaque()
    {
        StopActive();
        SetAlpha(1f);
    }

    /// <summary>Immediately sets the overlay to fully transparent.</summary>
    public void SetTransparent()
    {
        StopActive();
        SetAlpha(0f);
    }

    private void StartFade(float from, float to, float duration, Action onComplete)
    {
        StopActive();
        m_activeRoutine = StartCoroutine(FadeRoutine(from, to, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        m_overlay.raycastTarget = true; // Block clicks during fade.
        SetAlpha(from);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
        m_overlay.raycastTarget = to > 0.5f; // Keep blocking only if opaque.
        m_activeRoutine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        Color c = m_overlay.color;
        c.a = alpha;
        m_overlay.color = c;
    }

    private void StopActive()
    {
        if (m_activeRoutine != null)
        {
            StopCoroutine(m_activeRoutine);
            m_activeRoutine = null;
        }
    }
}

