using UnityEngine;

public class MovimentStateDraco : State<ControllerDraco>
{
    public MovimentStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Update()
    {
        // Pulo tem prioridade sobre parar
        if (Controller.JumpBuffer.HasBufferedInput && (Controller.Motor.IsGrounded || Controller.Motor.IsOnClimbSurface))
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        if (!Controller.HasMoveInput)
            Controller.SwitchState(Controller.IdleState);
    }

    public override void FixedUpdate()
    {
        if (Controller.Motor.IsOnClimbSurface)
            Controller.Motor.MoveOnSurface(true);
        else
            Controller.Motor.MoveOnGround(Controller.PlayerInput.Move);

    }
}
