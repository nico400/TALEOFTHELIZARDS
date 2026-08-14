using UnityEngine;

public class GetDirectionMoveKeyboard : IStrategyToGetDirectionInput 
{
    public Vector3 GetDirectionMove(Transform cam, InputReader inputReader) 
    {
        Vector2 input = inputReader.DirectionBoard;

        if (input.sqrMagnitude < 0.01f) return Vector3.zero;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y) + (right * input.x);

        return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
    }
}
