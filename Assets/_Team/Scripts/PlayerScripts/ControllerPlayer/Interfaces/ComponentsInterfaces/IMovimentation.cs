using UnityEngine;

public interface IMovimentation
{
    Vector3 Move(bool canMove, Vector3 velocity, Vector3 direction);
    Vector3 HorizontalVelocity();
}
