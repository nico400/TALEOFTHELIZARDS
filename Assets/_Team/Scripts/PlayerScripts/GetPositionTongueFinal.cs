using UnityEngine;

public class GetPositionTongueFinal : IGetPositionTongueFinal
{
    float distance;
    float radiusSphereCast;
    LayerMask layerToAttach;
    public GetPositionTongueFinal(float distance, float radiusCapsuleCast, LayerMask layerToAttach)
    {
        this.distance = distance;
        this.radiusSphereCast = radiusCapsuleCast;
        this.layerToAttach = layerToAttach;
    }
    public Vector3 PositionFinalTongue(Vector3 posStart, Vector3 direction)
    {
        RaycastHit sphereHit;
        Physics.SphereCast(posStart, radiusSphereCast, direction, out sphereHit, distance, layerToAttach);
        RaycastHit normalHit;
        Physics.Raycast(posStart, direction, out normalHit, distance, layerToAttach);

        Vector3 realHitPoint;

        // Opção 1 - Ray Hit
        if (normalHit.point != Vector3.zero)
            realHitPoint = normalHit.point;
        // Opção 2 - Sphere Hit
        else if (sphereHit.point != Vector3.zero)
            realHitPoint = sphereHit.point;
        // Opção 3 - errou
        else
            realHitPoint = Vector3.zero;

        return realHitPoint;
    }
}
