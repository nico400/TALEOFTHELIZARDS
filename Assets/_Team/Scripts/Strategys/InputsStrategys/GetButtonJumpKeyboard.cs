using UnityEngine;

public class GetButtonJumpKeyboard : IStrategyToGetButtonJumpInput 
{
    public bool GetButtonJumpRelease(InputReader inputReader) 
    {
        return inputReader.JumpBoardInputRelease;
    }

    public bool GetButtonJumpStart(InputReader inputReader) 
    {
        return inputReader.JumpBoardInputStart;
    }
}
