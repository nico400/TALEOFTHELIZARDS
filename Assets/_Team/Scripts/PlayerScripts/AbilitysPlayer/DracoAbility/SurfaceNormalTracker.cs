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

    public Vector3 CurrentNormal => m_lastKnownNormal;
    public bool HasNormal => m_hasNormal;

    public SurfaceNormalTracker(float graceTime)
    {
        m_graceTime = graceTime;
    }

    public void Update(SurfaceProbeResult probeResult, float deltaTime)
    {
        if (probeResult.WallHitFound && probeResult.WallHit.collider.CompareTag("Climb"))
        {
            SetNormal(probeResult.WallHit.normal);
        }
        else if (probeResult.GroundHitFound && probeResult.GroundHit.collider.CompareTag("Climb"))
        {
            SetNormal(probeResult.GroundHit.normal);
        }
        else
        {
            m_timeSinceLastHit += deltaTime;
            if (m_timeSinceLastHit >= m_graceTime)
            {
                m_lastKnownNormal = Vector3.up;
                m_hasNormal = false;
            }
        }
    }

    private void SetNormal(Vector3 normal)
    {
        m_lastKnownNormal = normal;
        m_hasNormal = true;
        m_timeSinceLastHit = 0f;
    }
}
