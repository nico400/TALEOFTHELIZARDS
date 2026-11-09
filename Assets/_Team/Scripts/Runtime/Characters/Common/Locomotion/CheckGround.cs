using UnityEngine;

public class CheckGround : ICheckGround 
{
    public bool IsGroundCheck(float distanceCheck, float radiusSphereCheck, Vector3 posToStartCheck, Vector3 dirToCheckGround, LayerMask layerCheckToIgnore) 
    {
        layerCheckToIgnore = ~layerCheckToIgnore;
        return Physics.SphereCast(posToStartCheck, radiusSphereCheck, dirToCheckGround, out _, distanceCheck, layerCheckToIgnore) ? true : false;
    }
}
