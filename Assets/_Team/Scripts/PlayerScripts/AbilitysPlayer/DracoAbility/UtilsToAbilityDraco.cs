using UnityEngine;

public static class UtilsToAbilityDraco 
{
    public static Vector3 NewOrientationPlayer(Transform playerTransform, Vector3 normal, Transform cam, Vector2 input) 
    {
        Vector3 refDir = cam.right;

        if (Mathf.Abs(Vector3.Dot(refDir, normal)) > 0.99f)
            refDir = cam.up;

        Vector3 _rightPlayer = Vector3.ProjectOnPlane(refDir, normal).normalized;
        Vector3 _forwardPlayer = Vector3.Cross(_rightPlayer, normal).normalized; // ordem corrigida

        Vector2 inputClamped = Vector2.ClampMagnitude(input, 1f); // evita diagonal mais rápida

        Vector3 dir = _forwardPlayer * inputClamped.y + _rightPlayer * inputClamped.x;

        Debug.DrawRay(playerTransform.position, _rightPlayer * 2f, Color.red);
        Debug.DrawRay(playerTransform.position, _forwardPlayer * 2f, Color.green);

        return dir;
    }
}
