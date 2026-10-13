using UnityEngine;

public class JumpStateDraco : State<ControllerDraco>
{
    private bool m_jumpCut; // true quando este pulo ja foi cortado

    public JumpStateDraco(ControllerDraco controller) : base(controller) { }

    public override void Enter()
    {
        Controller.JumpBuffer.Consume();
        Controller.Motor.Jump(Controller.Settings.JumpForce);
        m_jumpCut = false;
    }

    public override void Update()
    {
        // Da para agarrar a lingua no meio do pulo
        if (Controller.TryMountTongue()) return;

        // So pousa depois de parar de subir (evita sair no mesmo frame do pulo)
        bool _falling = Controller.RB.linearVelocity.y <= 0f;
        bool _landed = Controller.Motor.IsGrounded || Controller.Motor.IsOnClimbSurface;

        // TODO: trocar por FallState quando ele existir
        if (_falling && _landed)
            Controller.SwitchState(Controller.HasMoveInput ? Controller.MoveState : Controller.IdleState);
    }

    public override void FixedUpdate()
    {
        // Soltou o botão enquanto subia: corta o pulo uma única vez
        if (!Controller.JumpHeld && !m_jumpCut)
        {
            Controller.Motor.CutJump(Controller.Settings.JumpCutMultiplier);
            m_jumpCut = true;
        }

        // Logo apos pular da parede nao aplica controle no ar,
        // senao o MoveOnGround zera o impulso horizontal do pulo
        if (!Controller.Motor.IsDetached)
            Controller.Motor.MoveOnGround(Controller.PlayerInput.Move);
    }
}
