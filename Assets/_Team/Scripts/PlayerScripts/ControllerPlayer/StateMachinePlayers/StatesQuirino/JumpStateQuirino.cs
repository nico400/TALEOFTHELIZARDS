public class JumpStateQuirino : State<StateMachineControllerQuirino>
{
    private bool m_jumpCut; // true quando este pulo ja foi cortado

    public JumpStateQuirino(StateMachineControllerQuirino controller) : base(controller) { }

    public override void Enter()
    {
        Controller.JumpBuffer.Consume();
        Controller.Motor.Jump(Controller.Settings.JumpForce);
        m_jumpCut = false;
    }

    public override void Update()
    {
        // So pousa depois de parar de subir (evita sair no mesmo frame do pulo)
        bool _falling = Controller.RB.linearVelocity.y <= 0f;

        // TODO: trocar por FallState quando ele existir
        if (_falling && Controller.Motor.IsGrounded)
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

        Controller.Motor.MoveOnGround(Controller.PlayerInput.Move);
    }
}
