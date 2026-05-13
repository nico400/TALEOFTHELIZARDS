using UnityEngine;

public class Gravity : IGravity
{
    float gravityForce = -9.81f;
    float gravityMultiplier;
    Vector3 verticalDirection;

    public Gravity(float gravityForce, float gravityMultiplier)
    {
        this.gravityForce = gravityForce;
        this.gravityMultiplier = gravityMultiplier;
    }
    public Vector3 VerticalVelocity()
    {
        return verticalDirection;
    }
    public Vector3 ApplyGravity(bool inGrounded, Vector3 velocity)
    {
        if (inGrounded && velocity.y < 0f)
            velocity.y = -2f;
        else
            velocity.y += gravityForce * gravityMultiplier * Time.fixedDeltaTime;

        verticalDirection = new Vector3(0f, velocity.y, 0f);
        return velocity;
    }
}
