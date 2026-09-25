using UnityEngine;

public class StateMachineController : MonoBehaviour
{
    public SODataPlayer DataPlayer; 
    State m_currentState;

    private void Start() 
    {
        m_currentState = new IdleStatePlayer(this);
    }

    private void Update() 
    {
        m_currentState.Update();
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
