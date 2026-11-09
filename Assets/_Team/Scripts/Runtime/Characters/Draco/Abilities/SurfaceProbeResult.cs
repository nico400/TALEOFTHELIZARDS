using UnityEngine;

public struct SurfaceProbeResult
{
    public RaycastHit GroundHit;
    public RaycastHit WallHit;
    public bool GroundHitFound;
    public bool WallHitFound;

    // Distancia da casca do collider ate o ponto acertado (0 = encostado)
    public float GroundDistance;
    public float WallDistance;
}
