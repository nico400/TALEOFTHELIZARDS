using UnityEngine;

public class IdleStatePlayer : State
{
    public IdleStatePlayer(StateMachineControllerDraco controller) : base(controller) { }

    public override void Update()
    {
        // Pulo tem prioridade sobre andar
        if (Controller.JumpBuffer.HasBufferedInput && (Controller.Motor.IsGrounded || Controller.Motor.IsOnClimbSurface))
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        if (Controller.HasMoveInput)
            Controller.SwitchState(Controller.MoveState);
    }

    public override void FixedUpdate()
    {
        if (Controller.Motor.IsOnClimbSurface)
            Controller.Motor.MoveOnSurface(false);   // fica grudado na parede
        else
            Controller.Motor.MoveOnGround(Vector2.zero); // desacelera ate parar
    }
}
