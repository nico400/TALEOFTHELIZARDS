using UnityEngine;

public static class CameraRelative
{
    private const float k_deadZoneSqr = 0.01f;

    // Converte um input 2D (x = direita, y = frente) em uma direção no plano XZ relativa à câmera
    public static Vector3 ToWorld(Vector2 input, Transform cam)
    {
        if (input.sqrMagnitude < k_deadZoneSqr) return Vector3.zero;

        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }
}
