using UnityEngine;

// Em cima do Target de um JumpPoint: nao anda, so gira para escolher o proximo pulo.
public class PerchedStateQuirino : State<ControllerQuirino>
{
    public JumpPoint Point { get; private set; }

    public PerchedStateQuirino(ControllerQuirino controller) : base(controller) { }

    // Chamar antes do SwitchState
    public void SetPoint(JumpPoint point) => Point = point;

    public override void Enter()
    {
        Controller.Motor.ResetGroundVelocity();
        Controller.RB.useGravity = false;
        Controller.RB.linearVelocity = Vector3.zero;
        Controller.Animations.PlayPerched();
    }

    public override void Update()
    {
        // O objeto sumiu ou foi pego: volta a andar/cair normalmente
        if (Point == null || !Point.IsAvailable)
        {
            Controller.SwitchState(Controller.LocomotionState);
            return;
        }

        if (Controller.JumpBuffer.HasBufferedInput)
        {
            // Sem input pula para onde esta olhando (o jogador ja girou o Quirino)
            Vector3 _direction = Controller.HasMoveInput ? Controller.MoveDirection : Controller.transform.forward;
            Controller.SwitchState(Controller.ResolveJumpState(_direction, Point));
        }
    }

    public override void FixedUpdate()
    {
        if (Point == null || !Point.IsAvailable) return;

        // Fica preso no Target (e acompanha se o objeto se mexer)
        Controller.RB.linearVelocity = (Point.LandPosition - Controller.RB.position) / Time.fixedDeltaTime;
        Controller.Motor.RotateTowards(Controller.MoveDirection);
    }

    public override void Exit()
    {
        Controller.RB.useGravity = true;
        Controller.RB.linearVelocity = Vector3.zero; // nao leva a velocidade de "segurar" para o pulo
    }
}
