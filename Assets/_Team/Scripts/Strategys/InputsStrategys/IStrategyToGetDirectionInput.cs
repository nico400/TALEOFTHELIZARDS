using UnityEngine;

public interface IStrategyToGetDirectionInput
{
    Vector3 GetDirectionMove(Transform cam, InputReader inputReader);
}
