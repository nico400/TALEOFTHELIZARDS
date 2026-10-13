using UnityEngine;

// Lingua indo ate o ponto mirado. O Quirino fica parado.
public class TongueExtendStateQuirino : State<ControllerQuirino>
{
    public TongueExtendStateQuirino(ControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.Tongue.Shoot();
    }

    public override void Update()
    {
        Controller.Tongue.Draw();
    }

    public override void FixedUpdate()
    {
        Controller.Motor.StopOnGround(); // desacelera ate parar, sem rotacionar

        QuirinoTongue _tongue = Controller.Tongue;
        if (!_tongue.MoveTip(_tongue.AttachPoint, Controller.Settings.TongueExtendSpeed)) return;

        // Chegou: prende se for um alvo permitido, senao volta
        Controller.SwitchState(_tongue.HasTarget ? Controller.TongueAttachedState : Controller.TongueRetractState);
    }
}
