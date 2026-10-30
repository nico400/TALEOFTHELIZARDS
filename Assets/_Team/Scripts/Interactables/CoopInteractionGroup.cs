using System;
using UnityEngine;
using UnityEngine.Events;

// Junta os CoopInteractables que precisam estar em uso ao mesmo tempo (ex: um com o
// Draco e outro com o Quirino). Quando todos estao ocupados, ativa o evento.
// Como cada personagem so interage com um objeto por vez, dois objetos = dois personagens.
public class CoopInteractionGroup : MonoBehaviour
{
    [Tooltip("Objetos que precisam estar em uso juntos")]
    [SerializeField] private CoopInteractable[] m_interactables;
    [Tooltip("Depois de ativar ninguem mais interage com estes objetos")]
    [SerializeField] private bool m_oneShot = true;
    [Tooltip("Ao ativar solta os personagens. Desligado: continuam presos ate apertar Interagir")]
    [SerializeField] private bool m_releaseOnActivate = true;

    [SerializeField] private UnityEvent m_onActivated;
    [Tooltip("So sem Release On Activate: alguem saiu depois de ativar")]
    [SerializeField] private UnityEvent m_onDeactivated;

    public event Action<CoopInteractionGroup> Activated = delegate { };
    public event Action<CoopInteractionGroup> Deactivated = delegate { };

    public bool IsActive { get; private set; }
    public bool HasActivated { get; private set; }
    public bool AcceptsInteraction => isActiveAndEnabled && !(m_oneShot && HasActivated);

    // Soltar dentro do evento de comecar deixaria o PlayerInteractor no meio do TryStartInteraction
    private bool m_releasePending;

    private void Awake()
    {
        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable != null)
                _interactable.Group = this;
        }
    }

    private void OnEnable()
    {
        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable == null) continue;
            _interactable.InteractStarted += OnInteractStarted;
            _interactable.InteractStopped += OnInteractStopped;
        }
    }

    private void OnDisable()
    {
        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable == null) continue;
            _interactable.InteractStarted -= OnInteractStarted;
            _interactable.InteractStopped -= OnInteractStopped;
        }

        ReleaseAll();
    }

    private void Update()
    {
        if (!m_releasePending) return;
        m_releasePending = false;
        ReleaseAll();
    }

    // Solta todos os personagens presos neste grupo
    public void ReleaseAll()
    {
        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable != null)
                _interactable.StopInteraction();
        }
    }

    // Deixa ativar de novo (ex: puzzle que reinicia)
    public void ResetGroup()
    {
        HasActivated = false;
        IsActive = false;
        m_releasePending = false;
    }

    private void OnInteractStarted(Interactable interactable, PlayerInteractor interactor)
    {
        if (IsActive || !AreAllInteracting()) return;

        IsActive = true;
        HasActivated = true;
        m_onActivated.Invoke();
        Activated.Invoke(this);

        if (m_releaseOnActivate)
            m_releasePending = true;
    }

    private void OnInteractStopped(Interactable interactable, PlayerInteractor interactor)
    {
        if (!IsActive) return;
        IsActive = false;

        // Soltando por causa da ativacao nao e "desativar"
        if (m_releaseOnActivate) return;

        m_onDeactivated.Invoke();
        Deactivated.Invoke(this);
    }

    private bool AreAllInteracting()
    {
        if (m_interactables == null || m_interactables.Length == 0) return false;

        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable == null || !_interactable.IsInteracting) return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (m_interactables == null) return;

        Gizmos.color = IsActive ? Color.green : Color.cyan;
        foreach (CoopInteractable _interactable in m_interactables)
        {
            if (_interactable != null)
                Gizmos.DrawLine(transform.position, _interactable.InteractionPosition);
        }
    }
}
