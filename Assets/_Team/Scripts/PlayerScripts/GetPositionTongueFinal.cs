using UnityEngine;

public class GetPositionTongueFinal : IGetPositionTongueFinal
{
    float distance;
    float radiusSphereCast;
    string tagName;
    LayerMask layerToAttach;
    LayerMask layerObstacle;

    public GetPositionTongueFinal(float distance, float radiusCapsuleCast, string tagName, LayerMask layerToAttach, LayerMask layerObstacle)
    {
        this.distance = distance;
        this.radiusSphereCast = radiusCapsuleCast;
        this.tagName = tagName;
        this.layerToAttach = layerToAttach;
        this.layerObstacle = layerObstacle;
    }

    public Vector3 PositionFinalTongue(Vector3 posStart, Vector3 direction)
    {
        LayerMask combineMask = layerToAttach | layerObstacle;

        RaycastHit raycastHit;
        bool rayHit = Physics.Raycast(posStart, direction, out raycastHit, distance, combineMask);

        RaycastHit spherecastHit;
        bool sphereHit = Physics.SphereCast(posStart, radiusSphereCast, direction, out spherecastHit, distance, combineMask);

        if (sphereHit)
        {
            if (!spherecastHit.collider.CompareTag(tagName))
                return Vector3.zero;

            spherecastHit.collider.TryGetComponent<AttachObjects>(out AttachObjects at);
            return at.AttachPos.position;
        }

        else if (rayHit)
        {
            if (!raycastHit.collider.CompareTag(tagName))
                return Vector3.zero;

            raycastHit.collider.TryGetComponent<AttachObjects>(out AttachObjects at);
            return at.AttachPos.position;
        }

        return Vector3.zero;
    }
}