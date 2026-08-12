using UnityEngine;

public class ChangeInputJump 
{
    IStrategyToGetButtonJumpInput m_strategyInput;

    public void SetStrategyInput(IStrategyToGetButtonJumpInput strategyInput)
    {
        m_strategyInput = strategyInput;
    }
    public bool ExecuteJump(InputReader inputReader) 
    {
        if (m_strategyInput == null) 
        {
            return false;
        }
        return m_strategyInput.GetButtonJump(inputReader);
    }
}
