using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Personagens que podem usar um Interactable
[Flags]
public enum InteractorCharacter
{
    None = 0,
    Draco = 1 << 0,
    Quirino = 1 << 1,
}

// Objeto que o Draco e/ou o Quirino usam com Interagir (alavanca, botao, porta...).
// Distancia e se o personagem esta livre quem checa e o PlayerInteractor;
// aqui so fica o que acontece ao comecar e ao parar de interagir.
// Liga as acoes pelo Inspector ou pelos eventos em codigo; para logica propria herde
// e sobrescreva OnInteractStarted/OnInteractStopped/CanInteract.
public class Interactable : MonoBehaviour
{
    public enum InteractionMode
    {
        // Comeca e para no mesmo aperto (botao, pegar item)
        Instant,
        // Aperta para comecar e de novo para parar (alavanca segurada, conversa)
        Toggle,
    }

    // Todos os objetos ativos na cena: o PlayerInteractor procura aqui, sem precisar de collider
    private static readonly List<Interactable> s_active = new();
    public static IReadOnlyList<Interactable> Active => s_active;

    [SerializeField] private InteractionMode m_mode = InteractionMode.Instant;
    [SerializeField] private InteractorCharacter m_allowedCharacters = InteractorCharacter.Draco | InteractorCharacter.Quirino;

    [Tooltip("Distancia maxima do personagem ate o ponto de interacao")]
    [SerializeField, Min(0f)] private float m_interactDistance = 1.5f;
    [Tooltip("Ponto usado para medir a distancia. Vazio = posicao deste objeto")]
    [SerializeField] private Transform m_interactionPoint;
    [Tooltip("So Toggle: para a interacao quando o personagem se afasta")]
    [SerializeField] private bool m_stopWhenOutOfRange = true;

    [SerializeField] private UnityEvent m_onInteractStarted;
    [SerializeField] private UnityEvent m_onInteractStopped;

    // Para scripts que precisam saber quem interagiu
    public event Action<Interactable, PlayerInteractor> InteractStarted = delegate { };
    public event Action<Interactable, PlayerInteractor> InteractStopped = delegate { };

    public InteractionMode Mode => m_mode;
    public bool StopWhenOutOfRange => m_stopWhenOutOfRange;
    public Vector3 InteractionPosition => m_interactionPoint != null ? m_interactionPoint.position : transform.position;

    // Quem esta interagindo agora (so um por vez)
    public PlayerInteractor CurrentInteractor { get; private set; }
    public bool IsInteracting => CurrentInteractor != null;

    public float SqrDistanceTo(Vector3 position) => (InteractionPosition - position).sqrMagnitude;
    public bool IsInRange(Vector3 position) => SqrDistanceTo(position) <= m_interactDistance * m_interactDistance;

    // Sobrescreva para regras proprias (ex: porta trancada). Chame a base.
    public virtual bool CanInteract(PlayerInteractor interactor)
    {
        return isActiveAndEnabled && !IsInteracting && (m_allowedCharacters & interactor.Character) != 0;
    }

    // O proprio objeto pode encerrar a interacao (ex: animacao terminou)
    public void StopInteraction() => CurrentInteractor?.StopInteracting();

    protected virtual void OnEnable() => s_active.Add(this);

    protected virtual void OnDisable()
    {
        StopInteraction();
        s_active.Remove(this);
    }

    // Chamados so pelo PlayerInteractor, que guarda o lado dele do estado
    internal void BeginInteraction(PlayerInteractor interactor)
    {
        CurrentInteractor = interactor;
        OnInteractStarted(interactor);
        m_onInteractStarted.Invoke();
        InteractStarted.Invoke(this, interactor);
    }

    internal void EndInteraction(PlayerInteractor interactor)
    {
        if (CurrentInteractor != interactor) return;
        CurrentInteractor = null;
        OnInteractStopped(interactor);
        m_onInteractStopped.Invoke();
        InteractStopped.Invoke(this, interactor);
    }

    // Antes dos eventos de comecar
    protected virtual void OnInteractStarted(PlayerInteractor interactor) { }

    // Antes dos eventos de parar
    protected virtual void OnInteractStopped(PlayerInteractor interactor) { }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = IsInteracting ? Color.green : Color.magenta;
        Gizmos.DrawWireSphere(InteractionPosition, m_interactDistance);
    }
}
