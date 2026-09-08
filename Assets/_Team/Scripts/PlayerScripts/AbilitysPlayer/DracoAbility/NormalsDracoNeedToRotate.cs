using UnityEngine;

public class NormalsDracoNeedToRotate 
{
    float m_radiusSphereCast = 1.25f;
    float m_distanceRay = 1.25f;
    float m_distanceRaycast = 1.25f;
    float m_wallProbeRadius = 0.15f;
    LayerMask m_maskToNotDetectedHit = ~LayerMask.GetMask("Players");

    public DataNormalsDracoNeedToRotate GetNormalsDracoNeedToRotate(Transform playerTransform, Vector3 dirMoviment, Vector3 normalToUse) 
    {
        DataNormalsDracoNeedToRotate _data = new DataNormalsDracoNeedToRotate();
        float _offsetSphere = m_radiusSphereCast + 0.1f;
        float _offsetRay = 0.25f;
        Vector3 _originSphere = playerTransform.position + normalToUse * _offsetSphere;
        Vector3 _oringinRay = playerTransform.position + normalToUse * _offsetRay;

        _data.GroundHitFound = Physics.SphereCast
            (
                _originSphere,
                m_radiusSphereCast,
                -normalToUse,
                out _data.GroundHit,
                m_distanceRay,
                m_maskToNotDetectedHit
            );

        _data.WallHitFound = Physics.SphereCast
            (
                _oringinRay,
                m_wallProbeRadius,
                dirMoviment,
                out _data.WallHit,
                m_distanceRaycast,
                m_maskToNotDetectedHit
            );

        return _data;
    }
}
