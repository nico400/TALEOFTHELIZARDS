using System;
using UnityEngine;

// Parte comum do Interagir para o Draco e o Quirino: acha o Interactable mais perto,
// checa se o personagem esta livre, comeca/para a interacao e para sozinho se afastar.
// Cada controller diz quando o personagem esta ocupado pelo isAvailable.
public class PlayerInteractor
{
    private readonly Func<bool> m_isAvailable;

    public InteractorCharacter Character { get; }

    // Personagem dono deste interactor
    public Transform Owner { get; }

    // De onde a distancia e medida (ex: boca)
    public Transform Origin { get; }

    public Interactable Current { get; private set; }
    public bool IsInteracting => Current != null; // null tambem se o objeto foi destruido

    // Ocupado (lingua esticada, objeto na boca...) nao comeca interacao nova; parar sempre pode
    public bool IsAvailable => m_isAvailable == null || m_isAvailable();

    public event Action<Interactable> Started = delegate { };
    public event Action<Interactable> Stopped = delegate { };

    public PlayerInteractor(InteractorCharacter character, Transform owner, Transform origin, Func<bool> isAvailable)
    {
        Character = character;
        Owner = owner;
        Origin = origin != null ? origin : owner;
        m_isAvailable = isAvailable;
    }

    // Interactable mais perto que este personagem pode usar agora
    public Interactable FindNearest()
    {
        Vector3 _origin = Origin.position;
        Interactable _nearest = null;
        float _nearestSqr = float.PositiveInfinity;

        for (int i = 0; i < Interactable.Active.Count; i++)
        {
            Interactable _interactable = Interactable.Active[i];
            if (!_interactable.IsInRange(_origin) || !_interactable.CanInteract(this)) continue;

            float _sqr = _interactable.SqrDistanceTo(_origin);
            if (_sqr < _nearestSqr)
            {
                _nearestSqr = _sqr;
                _nearest = _interactable;
            }
        }

        return _nearest;
    }

    // Um aperto de Interagir: para a interacao atual ou comeca com o objeto mais perto.
    // Retorna true quando o aperto foi usado
    public bool TryInteract()
    {
        if (IsInteracting)
        {
            StopInteracting();
            return true;
        }

        return TryStartInteraction();
    }

    public bool TryStartInteraction()
    {
        if (IsInteracting || !IsAvailable) return false;

        Interactable _target = FindNearest();
        if (_target == null) return false;

        Current = _target;
        _target.BeginInteraction(this);
        Started.Invoke(_target);

        if (_target.Mode == Interactable.InteractionMode.Instant)
            StopInteracting();

        return true;
    }

    public void StopInteracting()
    {
        // Objeto destruido: o OnDisable dele ja encerrou, so limpa a referencia
        if (!IsInteracting)
        {
            Current = null;
            return;
        }

        Interactable _stopped = Current;
        Current = null;
        _stopped.EndInteraction(this);
        Stopped.Invoke(_stopped);
    }

    // Chamar no Update do controller
    public void Update()
    {
        if (IsInteracting && Current.StopWhenOutOfRange && !Current.IsInRange(Origin.position))
            StopInteracting();
    }
}
