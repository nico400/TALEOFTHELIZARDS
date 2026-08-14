using UnityEngine;

public interface ICheckGround
{
    bool IsGroundCheck(float distanceCheck, float radiusSphereCheck, Vector3 posToStartCheck, Vector3 dirToCheckGround, LayerMask layerCheckToIgnore);
}
