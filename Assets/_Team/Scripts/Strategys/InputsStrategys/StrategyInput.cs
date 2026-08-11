using UnityEngine;

public interface StrategyInput
{
    Vector3 GetDirectionMove(Transform cam, InputReader inputReader);
}
