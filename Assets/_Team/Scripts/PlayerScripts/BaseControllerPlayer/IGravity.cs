using UnityEngine;

public interface IGravity
{
    Vector3 ApplyGravity(bool inGrounded, Vector3 velocity);
    Vector3 VerticalVelocity();
}
