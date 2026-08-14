using UnityEngine;

public class Movimentation : IMovimentation
{
    private float m_maxSpeed;
    private float m_acceleration;
    private float m_deceleration;
    private float m_currentSpeed;
    private Vector3 m_horizontalDirection;

    public Movimentation(float speed, float acceleration, float deceleration)
    {
        m_maxSpeed = speed;
        m_acceleration = acceleration;
        m_deceleration = deceleration;
    }

    public Vector3 HorizontalVelocity()
    {
        return m_horizontalDirection * m_currentSpeed;
    }

    public Vector3 Move(bool canMove, Vector3 velocity, Vector3 direction)
    {
        if (!canMove) return velocity;

        if (direction.sqrMagnitude > 0.01f) 
        {
            m_currentSpeed += m_acceleration * Time.fixedDeltaTime;
            m_currentSpeed = Mathf.Min(m_currentSpeed, m_maxSpeed);

            m_horizontalDirection = direction.normalized;
        }
        else 
        {
            m_currentSpeed -= m_deceleration * Time.fixedDeltaTime;
            m_currentSpeed = Mathf.Max(m_currentSpeed, 0f);
        }

        velocity.x = m_horizontalDirection.x * m_currentSpeed;
        velocity.z = m_horizontalDirection.z * m_currentSpeed;

        return velocity;
    }
}
