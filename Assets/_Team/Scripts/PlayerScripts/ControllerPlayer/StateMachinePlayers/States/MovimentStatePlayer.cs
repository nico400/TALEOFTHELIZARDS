using UnityEngine;

public class MovimentStatePlayer : State 
{
    StateMachineController m_controller;

    public MovimentStatePlayer(StateMachineController controller) : base(controller) 
    {
        m_controller = controller;
    }

    public override void Enter() 
    {
        Debug.Log("movimentation");
    }   

    public override void Update() 
    {
        
    }

    public override void Exit() 
    {

    }
}
