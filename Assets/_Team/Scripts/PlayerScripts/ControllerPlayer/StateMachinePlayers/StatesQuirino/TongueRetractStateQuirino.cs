using UnityEngine;

// Lingua voltando para a boca. O Quirino fica parado.
public class TongueRetractStateQuirino : State<ControllerQuirino>
{
    public TongueRetractStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Tongue.Release();
        Controller.Motor.SetCameraOffset(Controller.Settings.AimCameraOffset);
    }

    public override void Exit()
    {
        Controller.Motor.ResetCameraOffset();
    }

    public override void Update()
    {
        Controller.Tongue.Draw();
    }

    public override void FixedUpdate()
    {
        Controller.Motor.StopOnGround();

        QuirinoTongue _tongue = Controller.Tongue;
        if (!_tongue.MoveTip(_tongue.OriginPosition, Controller.Settings.TongueRetractSpeed)) return;

        _tongue.Hide();
        Controller.SwitchState(Controller.CanAim ? Controller.AimState : Controller.LocomotionState);
    }
}
