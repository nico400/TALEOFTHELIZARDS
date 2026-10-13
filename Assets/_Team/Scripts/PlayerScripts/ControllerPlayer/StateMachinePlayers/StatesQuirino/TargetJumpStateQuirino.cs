using UnityEngine;

// Pulo direcionado: segue um arco ate o Target do JumpPoint e pousa em cima dele.
// A gravidade fica desligada; a trajetoria e calculada aqui.
public class TargetJumpStateQuirino : State<ControllerQuirino>
{
    private JumpPoint m_point;
    private Vector3 m_start;
    private float m_duration;
    private float m_progress; // 0 = saida, 1 = pousou

    public TargetJumpStateQuirino(ControllerQuirino controller) : base(controller) { }

    // Chamar antes do SwitchState
    public void SetTarget(JumpPoint point) => m_point = point;

    public override void Enter()
    {
        Controller.JumpBuffer.Consume();
        Controller.Motor.ResetGroundVelocity();
        Controller.RB.useGravity = false;

        m_start = Controller.RB.position;
        m_progress = 0f;

        float _flatDistance = Vector3.ProjectOnPlane(m_point.LandPosition - m_start, Vector3.up).magnitude;
        m_duration = Mathf.Max(Controller.Settings.TargetJumpMinDuration, _flatDistance / Controller.Settings.TargetJumpSpeed);
    }

    public override void FixedUpdate()
    {
        // O objeto sumiu no meio do pulo: cai normalmente
        if (m_point == null)
        {
            Controller.SwitchState(Controller.LocomotionState);
            return;
        }

        float _dt = Time.fixedDeltaTime;
        m_progress = Mathf.Min(1f, m_progress + _dt / m_duration);

        // Le o Target todo frame para acompanhar objetos que se mexem
        Vector3 _end = m_point.LandPosition;
        Controller.Motor.RotateTowards(_end - m_start);

        if (m_progress >= 1f)
        {
            Controller.RB.position = _end;
            Controller.RB.linearVelocity = Vector3.zero;
            Controller.PerchedState.SetPoint(m_point);
            Controller.SwitchState(Controller.PerchedState);
            return;
        }

        // Linha reta ate o alvo + arco (4t(1-t) vale 1 no meio do pulo)
        float _arc = Controller.Settings.TargetJumpArcHeight * 4f * m_progress * (1f - m_progress);
        Vector3 _next = Vector3.Lerp(m_start, _end, m_progress) + Vector3.up * _arc;

        // Por velocidade (e nao MovePosition) para continuar colidindo com o cenario
        Controller.RB.linearVelocity = (_next - Controller.RB.position) / _dt;
    }

    public override void Exit()
    {
        Controller.RB.useGravity = true;
    }
}
