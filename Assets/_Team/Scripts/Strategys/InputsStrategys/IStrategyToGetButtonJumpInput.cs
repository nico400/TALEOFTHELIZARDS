using UnityEngine;

public interface IStrategyToGetButtonJumpInput
{
   bool GetButtonJumpStart(InputReader inputReader);
   bool GetButtonJumpRelease(InputReader inputReader);
}
