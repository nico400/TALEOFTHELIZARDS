using UnityEngine;

public class UtilsToAbilityDraco 
{
    private const float k_minProjectionSqrMagnitude = 0.015f;

    public static Vector3 NewOrientationPlayer(Transform playerTransform, Transform cam, Vector3 normal, Vector2 input) 
    {
        Vector3 _camForwardOnPlane = GetValidReferenceOnPlane(cam, cam.forward, cam.up, cam.right, normal);

        Vector3 _forward = _camForwardOnPlane.normalized;
        Vector3 _right = Vector3.ProjectOnPlane(cam.right, normal).normalized;

        Debug.DrawRay(playerTransform.position, _right * 2, Color.green);
        Debug.DrawRay(playerTransform.position, _forward * 2, Color.yellow);

        Vector3 _dirMoviment = (_forward * input.y + _right * input.x).normalized;
        Debug.DrawRay(playerTransform.position, _dirMoviment * 2, Color.red);

        return _dirMoviment;
    }

    private static Vector3 GetValidReferenceOnPlane(Transform cam, Vector3 forward, Vector3 up, Vector3 right, Vector3 normal) 
    {
        Vector3 _fromForward = Vector3.ProjectOnPlane(forward, normal);
        if (_fromForward.sqrMagnitude >= k_minProjectionSqrMagnitude) 
        {
            return _fromForward;
        }

        Vector3 _fromUp = Vector3.ProjectOnPlane(up, normal);
        if (_fromUp.sqrMagnitude >= k_minProjectionSqrMagnitude) 
        {
            return _fromUp;
        }

        return Vector3.ProjectOnPlane(right, normal);
    }
}
