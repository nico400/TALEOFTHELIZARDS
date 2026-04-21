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

    public void Move(Vector2 direction, CharacterController ch)
    {
        if (direction != Vector2.zero)
        {
            // Calculate the movement direction based on the input direction
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

            // Convert the 2D input direction to a 3D movement direction
            horizontalDirection = new Vector3(direction.x, 0f, direction.y);
            ch.Move(horizontalDirection * currentSpeed * Time.deltaTime);
        }
        else
        {
            // If there is no input, decelerate the player
            currentSpeed -= deceleration * Time.deltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 0f);

            // Move the player in the last known direction
            ch.Move(horizontalDirection * currentSpeed * Time.deltaTime);
        }
    }
}
