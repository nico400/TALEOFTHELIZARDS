using UnityEngine;

public class ControllerQuirino : MonoBehaviour
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

    [Header("Tongue")]
    [Tooltip("Ponto de onde a lingua sai (boca). Vazio = centro do Quirino")]
    [SerializeField] private Transform m_tongueOrigin;
    [SerializeField] private LineRenderer m_tongueLine;
    public QuirinoTongue Tongue { get; private set; }
    private bool m_fireTongueRequested;

    public QuirinoMotor Motor { get; private set; }

    public bool HasMoveInput => PlayerInput.Move.sqrMagnitude >= k_inputDeadZone;
    public bool CanAim => PlayerInput.IsAiming && Motor.IsGrounded;

    // Estado para voltar quando sai da mira ou da lingua
    public State LocomotionState => HasMoveInput ? MoveState : IdleState;

    public IdleStateQuirino IdleState { get; private set; }
    public MovimentationStateQuirino MoveState { get; private set; }
    public JumpStateQuirino JumpState { get; private set; }
    public AimStateQuirino AimState { get; private set; }
    public TongueExtendStateQuirino TongueExtendState { get; private set; }
    public TongueAttachedStateQuirino TongueAttachedState { get; private set; }
    public TongueRetractStateQuirino TongueRetractState { get; private set; }

    private State m_currentState;

    private void Awake()
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_settings == null)
            m_settings = new QuirinoSettings();

        Motor = new QuirinoMotor(transform, m_rigidbody, m_camera, m_settings);
        Tongue = new QuirinoTongue(m_tongueOrigin != null ? m_tongueOrigin : transform, m_tongueLine, m_camera, m_settings);

        // cria os estados uma vez aqui para nao perder os paremetros
        IdleState = new IdleStateQuirino(this);
        MoveState = new MovimentationStateQuirino(this);
        JumpState = new JumpStateQuirino(this);
        AimState = new AimStateQuirino(this);
        TongueExtendState = new TongueExtendStateQuirino(this);
        TongueAttachedState = new TongueAttachedStateQuirino(this);
        TongueRetractState = new TongueRetractStateQuirino(this);
    }

    private void OnEnable()
    {
        m_inputReader.EnableGameplay();
        m_inputReader.JumpStarted += OnJumpStarted;
        m_inputReader.JumpCanceled += OnJumpCanceled;
        m_inputReader.FireTongue += OnFireTongue;
    }

    private void OnDisable()
    {
        m_inputReader.DisableGameplay();
        m_inputReader.JumpStarted -= OnJumpStarted;
        m_inputReader.JumpCanceled -= OnJumpCanceled;
        m_inputReader.FireTongue -= OnFireTongue;
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

    // Guarda o aperto ate o AimState usar (o botao pode ser apertado entre dois Updates)
    private void OnFireTongue(bool pressed) { if (pressed) m_fireTongueRequested = true; }

    public bool ConsumeFireTongue()
    {
        bool _requested = m_fireTongueRequested;
        m_fireTongueRequested = false;
        return _requested;
    }
}
