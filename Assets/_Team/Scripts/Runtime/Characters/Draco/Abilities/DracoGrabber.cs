using System;
using UnityEngine;

// Procura objetos perto da boca do Draco, pega e solta.
// Segurar nao e um estado: o Draco anda, pula e escala com o objeto na boca.
public class DracoGrabber
{
    private const int k_maxOverlaps = 16;

    private readonly Transform m_holdPoint;
    private readonly DracoSettings m_settings;
    private readonly Collider[] m_overlaps = new Collider[k_maxOverlaps];

    public Grabbable Held { get; private set; }
    public bool IsHolding => Held != null; // null tambem se o objeto foi destruido

    public event Action<Grabbable> Grabbed = delegate { };
    public event Action<Grabbable> Released = delegate { };

    public DracoGrabber(Transform holdPoint, DracoSettings settings)
    {
        m_holdPoint = holdPoint;
        m_settings = settings;
    }

    // Objeto pegavel mais perto da boca, dentro do GrabRadius
    public Grabbable FindNearest()
    {
        Vector3 _center = m_holdPoint.position;
        int _count = Physics.OverlapSphereNonAlloc(_center, m_settings.GrabRadius, m_overlaps,
            m_settings.GrabLayers, QueryTriggerInteraction.Collide);

        Grabbable _nearest = null;
        float _nearestSqr = float.PositiveInfinity;

        for (int i = 0; i < _count; i++)
        {
            Grabbable _grabbable = m_overlaps[i].GetComponentInParent<Grabbable>();
            if (_grabbable == null || !_grabbable.CanBeGrabbed) continue;

            float _sqr = (m_overlaps[i].ClosestPoint(_center) - _center).sqrMagnitude;
            if (_sqr < _nearestSqr)
            {
                _nearestSqr = _sqr;
                _nearest = _grabbable;
            }
        }

        return _nearest;
    }

    public bool TryGrabNearest()
    {
        if (IsHolding) return false;

        Grabbable _grabbable = FindNearest();
        if (_grabbable == null) return false;

        Held = _grabbable;
        Held.Grab(m_holdPoint);
        Grabbed.Invoke(Held);
        return true;
    }

    public void Release(Vector3 velocity)
    {
        if (!IsHolding)
        {
            Held = null;
            return;
        }

        Grabbable _released = Held;
        Held = null;
        _released.Release(velocity);
        Released.Invoke(_released);
    }
}
