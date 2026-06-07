using UnityEngine;
using static Unity.Cinemachine.CinemachineImpulseManager.ImpulseEvent;

public class GamepadInputStrategy : IPlayerInputStrategy 
{
    public Vector3 GetDirectionMove(Transform cameraTransform, InputReader reader)
    {
        // Calcular a direção do movimento com base nos vetores forward e right da câmera
        Vector3 dir = cameraTransform.forward * reader.DirectionPad.y +
            cameraTransform.right * reader.DirectionPad.x;
        dir.y = 0f;
        dir = dir.normalized;

        return dir;
    }
}
