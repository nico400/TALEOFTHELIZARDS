using UnityEngine;

public interface IMovimentation
{
    Vector3 Move(bool canMove, Vector3 velocity, Vector2 direction);
    Vector3 HorizontalVelocity();
}
