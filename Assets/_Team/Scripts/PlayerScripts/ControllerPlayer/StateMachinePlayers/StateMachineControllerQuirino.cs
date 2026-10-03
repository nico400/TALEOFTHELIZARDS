using UnityEngine;

public class StateMachineControllerQuirino : MonoBehaviour
{
    private const float k_inputDeadZone = 0.1f;

    [SerializeField] private InputReader m_inputReader;
    public IPlayerInput PlayerInput => m_inputReader;

    [SerializeField] private Rigidbody m_rigidbody;
    public Rigidbody RB => m_rigidbody;

    [SerializeField] private Transform m_camera;
    public Transform Camera => m_camera;

    [SerializeField] private QuirinoSettings m_settings = new();
    public QuirinoSettings Settings => m_settings;

    [SerializeField] private TimedInputBuffer m_jumpBuffer = new();
    public TimedInputBuffer JumpBuffer => m_jumpBuffer;
    public bool JumpHeld { get; private set; }

    public QuirinoMotor Motor { get; private set; }

    public bool HasMoveInput => PlayerInput.Move.sqrMagnitude >= k_inputDeadZone;

    public IdleStateQuirino IdleState { get; private set; }
    public MovimentationStateQuirino MoveState { get; private set; }
    public JumpStateQuirino JumpState { get; private set; }

    private State m_currentState;

    private void Awake()
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_settings == null)
            m_settings = new QuirinoSettings();

        Motor = new QuirinoMotor(transform, m_rigidbody, m_camera, m_settings);

        // cria os estados uma vez aqui para nao perder os paremetros
        IdleState = new IdleStateQuirino(this);
        MoveState = new MovimentationStateQuirino(this);
        JumpState = new JumpStateQuirino(this);
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
        Motor.UpdateGround();
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
