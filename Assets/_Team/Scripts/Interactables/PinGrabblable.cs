using System;
using UnityEngine;
using UnityEngine.Events;

// Pino que o Draco carrega e finca: ao soltar, lanca um raio para baixo do pino
// (-transform.up) e, se acertar uma das PinLayers, fica preso no ponto atingido.
// Pegar de novo solta o pino para reposicionar.
public class PinGrabblable : Grabbable
{
    private const int k_maxHits = 8;

    [Header("Pin")]
    [Tooltip("Layers onde o pino pode ser fincado")]
    [SerializeField] private LayerMask m_pinLayers;
    [Tooltip("Distancia maxima do pino ate a superficie para fincar")]
    [SerializeField] private float m_pinDistance = 1f;
    [Tooltip("Gira o pino para ficar em pe na superficie (up = normal)")]
    [SerializeField] private bool m_alignToSurface = true;

    [SerializeField] private UnityEvent m_onPinned;
    [SerializeField] private UnityEvent m_onUnpinned;

    public event Action<PinGrabblable> Pinned = delegate { };
    public event Action<PinGrabblable> Unpinned = delegate { };

    public bool IsPinned { get; private set; }
    public RaycastHit PinHit { get; private set; }

    private readonly RaycastHit[] m_hits = new RaycastHit[k_maxHits];
    private bool m_wasKinematicBeforePin;

    // Tenta fincar onde esta agora. Pode ser chamado de fora (ex: pino ja comeca fincado)
    public bool TryFixedPin()
    {
        if (IsHeld || IsPinned) return false;
        if (!TryFindSurface(out RaycastHit _hit)) return false;

        if (m_alignToSurface)
            transform.rotation = Quaternion.FromToRotation(transform.up, _hit.normal) * transform.rotation;
        transform.position = _hit.point;

        if (Body != null)
        {
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            m_wasKinematicBeforePin = Body.isKinematic;
            Body.isKinematic = true; // nao cai nem e empurrado enquanto fincado
        }

        PinHit = _hit;
        IsPinned = true;
        m_onPinned.Invoke();
        Pinned.Invoke(this);
        return true;
    }

    protected override void OnGrab() => Unpin();

    protected override void OnRelease() => TryFixedPin();

    // Nao fica fincado no ar depois de voltar para o inicio
    protected override void OnResetToStart() => Unpin();

    // Volta a fisica de antes de fincar; o Grab salva esse estado logo depois
    private void Unpin()
    {
        if (!IsPinned) return;
        IsPinned = false;

        if (Body != null)
            Body.isKinematic = m_wasKinematicBeforePin;

        m_onUnpinned.Invoke();
        Unpinned.Invoke(this);
    }

    // Raio mais proximo que nao seja o proprio pino
    private bool TryFindSurface(out RaycastHit closest)
    {
        closest = default;
        int _count = Physics.RaycastNonAlloc(transform.position, -transform.up, m_hits,
            m_pinDistance, m_pinLayers, QueryTriggerInteraction.Ignore);

        float _closestDistance = float.PositiveInfinity;
        for (int i = 0; i < _count; i++)
        {
            if (m_hits[i].transform.IsChildOf(transform)) continue;

            if (m_hits[i].distance < _closestDistance)
            {
                _closestDistance = m_hits[i].distance;
                closest = m_hits[i];
            }
        }

        return _closestDistance < float.PositiveInfinity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsPinned ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * m_pinDistance);
    }
}
