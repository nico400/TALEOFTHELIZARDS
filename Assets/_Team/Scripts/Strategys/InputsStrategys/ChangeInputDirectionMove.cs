using UnityEngine;

public class ChangeInputDirectionMove 
{
    IStrategyToGetDirectionInput m_strategyInput;

    public void SetStrategyInput(IStrategyToGetDirectionInput strategyInput)
    {
        m_strategyInput = strategyInput;
    }

    public Vector3 GetDirectionMove(Transform cam, InputReader inputReader) 
    {
        if (m_strategyInput == null) 
        {
            return Vector3.zero; // Retorna Vector3.zero aqui também
        }
        return m_strategyInput.GetDirectionMove(cam, inputReader);
    }
}
