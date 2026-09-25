using UnityEngine;

// Detects nearby wall and ground surfaces via sphere casts.
// Generic by design: not tied to any specific character, so other
// climbing-capable abilities can reuse it.
public class WallAndGroundSurfaceProbe
{
    private readonly DracoWallClimbSettings m_settings;
    private readonly LayerMask m_layerToDetect = ~LayerMask.GetMask("Players");

    public WallAndGroundSurfaceProbe(DracoWallClimbSettings settings)
    {
        m_settings = settings;
    }

    public SurfaceProbeResult Probe(Transform playerTransform, Vector3 dirMoviment, Vector3 normalToUse)
    {
        SurfaceProbeResult _data = new SurfaceProbeResult();

        float _offsetColisionGround = m_settings.GroundProbeRadius + 0.1f; // offset to avoid self-collision when casting the sphere
        float _offsetColisionWall = m_settings.WallProbeRadius + 0.1f; // offset to avoid self-collision when casting the ray

        Vector3 _originGroundCast = playerTransform.position + normalToUse * _offsetColisionGround;
        Vector3 _oringinWallCast = playerTransform.position + normalToUse * _offsetColisionWall;

        float _pullBack = m_settings.WallProbeRadius + 0.025f;
        Vector3 _originWallCastSafe = _oringinWallCast - dirMoviment * _pullBack;

        _data.WallHitFound = Physics.SphereCast
            (
                _originWallCastSafe,
                m_settings.WallProbeRadius,
                dirMoviment,
                out _data.WallHit,
                m_settings.WallProbeDistance + _pullBack,
                m_layerToDetect
            );

        if (!_data.WallHitFound)
        {
            _data.GroundHitFound = Physics.SphereCast
            (
                _originGroundCast,
                m_settings.GroundProbeRadius,
                -normalToUse,
                out _data.GroundHit,
                m_settings.GroundProbeDistance,
                m_layerToDetect
            );
        }

        return _data;
    }
}
