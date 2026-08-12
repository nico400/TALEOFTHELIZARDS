using UnityEngine;

public interface IJump
{
    void StartJump(Rigidbody rb, InputReader inputReader, bool isJump, bool jumpPressed);
    void RealeaseJump(Rigidbody rb, InputReader inputReader, bool jumpPressed);
    void Jump(Rigidbody rb, bool isGround, out bool inJump, InputReader inputReader, bool jumpPressed);
}
