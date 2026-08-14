using UnityEngine;

public class GetButtonJumpKeyboard : IStrategyToGetButtonJumpInput 
{
    public bool GetButtonJump(InputReader inputReader) 
    {
        return inputReader.JumpBoardInput;
    }
}
