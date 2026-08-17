using UnityEngine;

public class GetButtonJumpGamepad : IStrategyToGetButtonJumpInput 
{
    public bool GetButtonJumpRelease(InputReader inputReader) 
    {
        return inputReader.JumpPadInputRelease;
    }

    public bool GetButtonJumpStart(InputReader inputReader) 
    {
        return inputReader.JumpPadInputStart;
    }
}
