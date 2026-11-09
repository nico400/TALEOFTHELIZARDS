using UnityEngine;

public class IdleStateQuirino : State<ControllerQuirino>
{
    public IdleStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Animations.PlayIdle();
    }

    public override void Update()
    {
        // Pulo tem prioridade sobre andar
        if (Controller.JumpBuffer.HasBufferedInput && Controller.Motor.IsGrounded)
        {
            Controller.SwitchState(Controller.ResolveJumpState(Controller.MoveDirection));
            return;
        }

        if (Controller.CanAim)
        {
            Controller.SwitchState(Controller.AimState);
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
