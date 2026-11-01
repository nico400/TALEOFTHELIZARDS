using UnityEngine;

public class IdleStateDraco : State<ControllerDraco>
{
    public IdleStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Animations.PlayIdle();
    }

    public override void Update()
    {
        if (Controller.TryInteract()) return;

        // Parado na parede quem cuida e o ClimbState
        if (Controller.Motor.IsOnClimbSurface)
        {
            Controller.SwitchState(Controller.ClimbState);
            return;
        }

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
