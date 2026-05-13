using UnityEngine;

public class Movimentation : IMovimentation
{
    float maxSpeed;
    float acceleration;
    float deceleration;
    float currentSpeed;
    Vector3 horizontalDirection;
    public Movimentation(float speed, float acceleration, float deceleration)
    {
        this.maxSpeed = speed;
        this.acceleration = acceleration;
        this.deceleration = deceleration;
    }

    public Vector3 HorizontalVelocity()
    {
        return horizontalDirection * currentSpeed;
    }

    public Vector3 Move(bool canMove, Vector3 velocity, Vector2 direction)
    {
        if (!canMove) return velocity;

        if (direction != Vector2.zero)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            horizontalDirection = new Vector3(direction.x, 0f, direction.y).normalized;
            velocity.x = horizontalDirection.x * currentSpeed;
            velocity.z = horizontalDirection.z * currentSpeed;
        }
        else
        {
            currentSpeed -= deceleration * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 0f);

            velocity.x = horizontalDirection.x * currentSpeed;
            velocity.z = horizontalDirection.z * currentSpeed;
        }

        return velocity;
    }
}
