using UnityEngine;

public interface IJump
{
    bool Jump(CharacterController ch, bool isJumping);
    void JumpInitialize(Vector3 pointEnd, Vector3 pointStart, bool isJumping);
}
