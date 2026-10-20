using UnityEngine;

// Remembers the last surface normal the player was attached to, and holds
// onto it for a short grace period after losing contact (e.g. briefly
// airborne between two wall segments) before falling back to world-up.
public class SurfaceNormalTracker
{
    private readonly float m_graceTime;

    private Vector3 m_lastKnownNormal = Vector3.up;
    private bool m_hasNormal;
    private float m_timeSinceLastHit;
    private float m_distance = float.PositiveInfinity;

    public Vector3 CurrentNormal => m_lastKnownNormal;
    public bool HasNormal => m_hasNormal;
    // Distancia ate a superficie escalavel deste frame (infinito durante o grace time)
    public float CurrentDistance => m_distance;

    public SurfaceNormalTracker(float graceTime)
    {
        m_graceTime = graceTime;
    }

    // Parede (cast para frente) tem prioridade sobre o chao, igual ao Spider de referencia
    public void Update(SurfaceProbeResult probeResult, float deltaTime)
    {
        if (probeResult.WallHitFound && probeResult.WallHit.collider.CompareTag("Climb"))
        {
            SetNormal(probeResult.WallHit.normal, probeResult.WallDistance);
        }
        else if (probeResult.GroundHitFound && probeResult.GroundHit.collider.CompareTag("Climb"))
        {
            SetNormal(probeResult.GroundHit.normal, probeResult.GroundDistance);
        }
        else
        {
            m_distance = float.PositiveInfinity;
            m_timeSinceLastHit += deltaTime;
            if (m_timeSinceLastHit >= m_graceTime)
            {
                m_lastKnownNormal = Vector3.up;
                m_hasNormal = false;
            }
        }
    }

    // Esquece a superficie atual (ex.: ao pular da parede)
    public void Reset()
    {
        m_lastKnownNormal = Vector3.up;
        m_hasNormal = false;
        m_timeSinceLastHit = 0f;
        m_distance = float.PositiveInfinity;
    }

    private void SetNormal(Vector3 normal, float distance)
    {
        m_lastKnownNormal = normal;
        m_hasNormal = true;
        m_timeSinceLastHit = 0f;
        m_distance = distance;
    }
}
