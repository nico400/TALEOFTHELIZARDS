using UnityEngine;

public class Gravity : IGravity
{
    float m_gravityForce = -9.81f;
    float m_gravityMultiplier;
    Vector3 m_verticalDirection;

    public Gravity(float gravityForce, float gravityMultiplier)
    {
        this.m_gravityForce = gravityForce;
        this.m_gravityMultiplier = gravityMultiplier;
    }
    public Vector3 VerticalVelocity()
    {
        return m_verticalDirection;
    }
    public Vector3 ApplyGravity(bool inGrounded, Vector3 velocity)
    {
        if (inGrounded && velocity.y < 0f)
            velocity.y = -2f;
        else
            velocity.y += m_gravityForce * m_gravityMultiplier * Time.fixedDeltaTime;

        m_verticalDirection = new Vector3(0f, velocity.y, 0f);
        return velocity;
    }
}
