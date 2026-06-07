using UnityEngine;

public interface IPlayerInputStrategy
{
    Vector3 GetDirectionMove(Transform cameraTransform, InputReader reader);
}
