using UnityEngine;

public class StateMachineController : MonoBehaviour
{
    State m_currentState;

    private void Start() 
    {
        
    }

    private void Update() 
    {
        
    }

    public void SwitchState(State newState) 
    {
        if (m_currentState == newState) return;
        if (m_currentState == null) return;

        m_currentState.Exit();
        m_currentState = newState;
        m_currentState.Enter();
    }
}
