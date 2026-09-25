using UnityEngine;

public interface IJump
{
    void StartJump(Rigidbody rb, bool isGround, TimedInputBuffer jumpBufferRelease, bool jumpButtonRelease);
    void RealeaseJump(Rigidbody rb, TimedInputBuffer jumpBufferStart, bool jumpButtonStart);
    void Jump(Rigidbody rb, bool isGround, TimedInputBuffer jumpBufferStart, TimedInputBuffer jumpBufferRelase, bool jumpButtonStart, bool jumpButtonRelease);
}
