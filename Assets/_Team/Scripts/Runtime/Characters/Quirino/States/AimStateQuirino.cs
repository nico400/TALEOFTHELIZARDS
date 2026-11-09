// Segurando a mira: anda normalmente, mas sempre olha para frente da camera
public class AimStateQuirino : State<ControllerQuirino>
{
    public AimStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.ConsumeFireTongue(); // descarta tiro apertado antes de mirar
        Controller.Motor.SetCameraOffset(Controller.Settings.AimCameraOffset);
        Controller.AimHUD.SetActive(true);
        Controller.Animations.PlayAim();
    }

    public override void Update()
    {
        if (!Controller.CanAim)
        {
            Controller.SwitchState(Controller.LocomotionState);
            return;
        }

        if (Controller.ConsumeFireTongue())
            Controller.SwitchState(Controller.TongueExtendState);
    }

    public override void FixedUpdate()
    {
        Controller.Motor.MoveOnGround(Controller.PlayerInput.Move, Controller.Camera.forward);
    }

    // Se o proximo estado tambem mira, ele sobe a camera de novo no Enter
    public override void Exit()
    {
        Controller.AimHUD.SetActive(false);
        Controller.Motor.ResetCameraOffset();
    }
}
