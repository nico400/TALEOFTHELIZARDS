using UnityEngine;

public class GetButtonJumpGamepad : IStrategyToGetButtonJumpInput 
{
    public bool GetButtonJump(InputReader inputReader) 
    {
        return inputReader.JumpPadInput;
    }
}
