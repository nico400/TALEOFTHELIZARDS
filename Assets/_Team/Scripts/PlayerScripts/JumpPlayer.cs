using UnityEngine;
using UnityEngine.Windows;

public class JumpPlayer : IJump 
{
    private float m_jumpForce;
    private float m_jumpMultiplier;


    public JumpPlayer(float jumpForce, float jumpMultiplier) 
    {
        m_jumpForce = jumpForce;
        m_jumpMultiplier = jumpMultiplier;
    }

    public void Jump(Rigidbody rb, bool isGround, out bool inJump, InputReader inputReader, bool jumpPressed) 
    {
        StartJump(rb, inputReader, isGround, jumpPressed);
        RealeaseJump(rb, inputReader, jumpPressed);

        inJump = true;
        if (inJump && isGround) 
        {
            inJump = false;
        }
    }

    public void RealeaseJump(Rigidbody rb, InputReader inputReader, bool jumpPressed) 
    {
        if (rb.linearVelocity.y > 0 && jumpPressed) 
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * m_jumpMultiplier);
        }         
    }

    public void StartJump(Rigidbody rb, InputReader inputReader, bool isGround, bool jumpPressed) 
    {
        if (isGround && jumpPressed) 
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, m_jumpForce);
        }           
    }
}
