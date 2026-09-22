using UnityEngine;

public class NormalsDracoNeedToRotate {

    // radius of the sphere cast to detect ground and wall collisions
    float m_radiusSphereCast = 0.75f;
    float m_wallProbeRadius;

    // distance of the raycast to detect ground and wall collisions
    float m_distanceGroundCast = 2.35f;
    float m_distanceWallCast = 1.35f;

    // layer to ignore when detecting collisions (e.g., Walkables)
    LayerMask m_layerToDetect = LayerMask.GetMask("Walkables");

    public DataNormalsDracoNeedToRotate GetNormalsDracoNeedToRotate(Transform playerTransform, Vector3 dirMoviment, Vector3 normalToUse) 
    {
        DataNormalsDracoNeedToRotate _data = new DataNormalsDracoNeedToRotate();
        m_wallProbeRadius = m_radiusSphereCast - 0.55f;

        float _offsetColisionGround = m_radiusSphereCast + 0.1f; // offset to avoid self-collision when casting the sphere
        float _offsetColisionWall = m_wallProbeRadius + 0.1f; // offset to avoid self-collision when casting the ray

        Vector3 _originGroundCast = playerTransform.position + normalToUse * _offsetColisionGround;
        Vector3 _oringinWallCast = playerTransform.position + normalToUse * _offsetColisionWall;

        float _pullBack = m_wallProbeRadius + 0.025f;
        Vector3 _originWallCastSafe = _oringinWallCast - dirMoviment * _pullBack;

        _data.WallHitFound = Physics.SphereCast
            (
                _originWallCastSafe,
                m_wallProbeRadius,
                dirMoviment,
                out _data.WallHit,
                m_distanceWallCast + _pullBack,
                m_layerToDetect
            );

        if (!_data.WallHitFound) 
        {
            _data.GroundHitFound = Physics.SphereCast
            (
                _originGroundCast,
                m_radiusSphereCast,
                -normalToUse,
                out _data.GroundHit,
                m_distanceGroundCast,
                m_layerToDetect
            );
        }

        return _data;
    }
}
