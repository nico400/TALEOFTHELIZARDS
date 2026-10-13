using UnityEngine;

public class ControllerDraco : MonoBehaviour
{
    private const float k_inputDeadZone = 0.1f;

    [SerializeField] private InputReader m_inputReader;
    public IPlayerInput PlayerInput => m_inputReader;

    [SerializeField] private Rigidbody m_rigidbody;
    public Rigidbody RB => m_rigidbody;

    [SerializeField] private Transform m_camera;
    public Transform Camera => m_camera;

    [SerializeField] private DracoSettings m_settingsClimb;
    public DracoSettings Settings => m_settingsClimb;

    [SerializeField] private TimedInputBuffer m_jumpBuffer = new();
    public TimedInputBuffer JumpBuffer => m_jumpBuffer;
    public bool JumpHeld { get; private set; }

    public DracoMotor Motor { get; private set; }

    public bool HasMoveInput => PlayerInput.Move.sqrMagnitude >= k_inputDeadZone;

    public IdleStateDraco IdleState { get; private set; }
    public MovimentStateDraco MoveState { get; private set; }
    public JumpStateDraco JumpState { get; private set; }

    private State m_currentState;

    private void Awake()
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_settingsClimb == null)
            m_settingsClimb = new DracoSettings();

        Motor = new DracoMotor(transform, m_rigidbody, m_camera, m_settingsClimb);
        
        // cria os estados uma vez aqui para nao perder os paremetros
        IdleState = new IdleStateDraco(this);
        MoveState = new MovimentStateDraco(this);
        JumpState = new JumpStateDraco(this);
    }

    private void OnEnable()
    {
        m_inputReader.EnableGameplay();
        m_inputReader.JumpStarted += OnJumpStarted;
        m_inputReader.JumpCanceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        m_inputReader.DisableGameplay();
        m_inputReader.JumpStarted -= OnJumpStarted;
        m_inputReader.JumpCanceled -= OnJumpCanceled;
    }

    private void Start()
    {
        SwitchState(IdleState); // começa no Idle
    }

    private void Update()
    {
        m_currentState?.Update();
    }

    private void FixedUpdate()
    {
        Motor.UpdateSurface(PlayerInput.Move);
        m_currentState?.FixedUpdate();
    }

    public void SwitchState(State newState)
    {
        if (newState == null || m_currentState == newState) return;

        m_currentState?.Exit();
        m_currentState = newState;
        m_currentState.Enter();
    }

    private void OnJumpStarted() { m_jumpBuffer.Set(); JumpHeld = true; }
    private void OnJumpCanceled() { JumpHeld = false; }

}
