using UnityEngine;
using UnityEngine.Windows;

public class JumpPlayer : IJump 
{
    private float m_jumpForce;
    private float m_jumpMultiplier;
    private bool m_hasReleasedCurrentJump;

    public JumpPlayer(float jumpForce, float jumpMultiplier) 
    {
        m_jumpForce = jumpForce;
        m_jumpMultiplier = jumpMultiplier;
    }

    public void Jump(Rigidbody rb, bool isGround, TimedInputBuffer jumpBufferStart, TimedInputBuffer jumpBufferRelease, bool jumpButtonStart, bool jumpButtonRelease) 
    {
        StartJump(rb, isGround, jumpBufferStart, jumpButtonStart);
        RealeaseJump(rb, jumpBufferRelease, jumpButtonRelease);
    }

    public void RealeaseJump(Rigidbody rb, TimedInputBuffer jumpBufferRelease, bool releaseButtonJump) 
    {
        if (rb.linearVelocity.y > 0 && !m_hasReleasedCurrentJump)
        {
            if (releaseButtonJump) {
                jumpBufferRelease.Set();
            }
            TryConsumeBufferedReleaseJump(rb, jumpBufferRelease);
        }
    }

    public void StartJump(Rigidbody rb, bool isGround, TimedInputBuffer jumpBuffer, bool startButtonJump) 
    {
        if (isGround) 
        {
            m_hasReleasedCurrentJump = false;
        }

        if (startButtonJump) 
        {
            jumpBuffer.Set();
        }
        TryConsumeBufferedStartJump(rb, isGround, jumpBuffer);
    }

    void TryConsumeBufferedStartJump(Rigidbody rb, bool isGround, TimedInputBuffer jumpBuffer) 
    {
        if (!isGround || !jumpBuffer.HasBufferedInput) 
        {
            return;
        }

        var velocity = rb.linearVelocity;
        velocity.y = m_jumpForce;
        rb.linearVelocity = velocity;

        jumpBuffer.Consume();
    }

    void TryConsumeBufferedReleaseJump(Rigidbody rb, TimedInputBuffer jumpBuffer) {
        if (!jumpBuffer.HasBufferedInput) 
        {
            return;
        }

        var velocity = rb.linearVelocity;
        velocity.y = m_jumpForce * m_jumpMultiplier;
        rb.linearVelocity = velocity;

        jumpBuffer.Consume();
        m_hasReleasedCurrentJump = true;
    }
}
