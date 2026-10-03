using UnityEngine;

public class IdleStateQuirino : State<StateMachineControllerQuirino>
{
    public IdleStateQuirino(StateMachineControllerQuirino controller) : base(controller) { }

    public override void Update()
    {
        // Pulo tem prioridade sobre andar
        if (Controller.JumpBuffer.HasBufferedInput && Controller.Motor.IsGrounded)
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        if (Controller.HasMoveInput)
            Controller.SwitchState(Controller.MoveState);
    }

    public override void FixedUpdate()
    {
        Controller.Motor.MoveOnGround(Vector2.zero); // desacelera ate parar
    }
}
