using UnityEngine;

public abstract class State 
{
    StateMachineController m_controller;
    public State(StateMachineController controller) 
    {
        m_controller = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
