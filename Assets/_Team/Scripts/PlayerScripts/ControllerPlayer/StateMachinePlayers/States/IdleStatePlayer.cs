using UnityEngine;

public class IdleStatePlayer : State 
{
    StateMachineController m_controller;

    public IdleStatePlayer(StateMachineController controller) : base(controller) 
    {
        m_controller = controller;
    }

    public override void Enter() 
    {
        Debug.Log("Idle");
    }

    public override void Update() 
    {
        if (m_controller == null) return;

    }

    public override void Exit() 
    {

    }
}

