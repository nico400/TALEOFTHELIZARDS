public class MovimentationStateQuirino : State<ControllerQuirino>
{
    public MovimentationStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Update()
    {
        // Pulo tem prioridade sobre parar
        if (Controller.JumpBuffer.HasBufferedInput && Controller.Motor.IsGrounded)
        {
            Controller.SwitchState(Controller.JumpState);
            return;
        }

        if (Controller.CanAim)
        {
            Controller.SwitchState(Controller.AimState);
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
