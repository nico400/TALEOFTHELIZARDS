using UnityEngine;

// Draco andando em superficie normal (sem escalar). Achou parede escalavel: vai para o ClimbState.
public class MovimentStateDraco : State<ControllerDraco>
{
    public MovimentStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Update()
    {
        if (Controller.TryInteract()) return;

        if (Controller.Motor.IsOnClimbSurface)
        {
            Controller.SwitchState(Controller.ClimbState);
            return;
        }

        // Pulo tem prioridade sobre parar
        if (Controller.JumpBuffer.HasBufferedInput && Controller.Motor.IsGrounded)
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        if (!Controller.HasMoveInput)
            Controller.SwitchState(Controller.IdleState);
    }

    public override void FixedUpdate()
    {
        Controller.Motor.MoveOnGround(Controller.PlayerInput.Move);
    }
}
