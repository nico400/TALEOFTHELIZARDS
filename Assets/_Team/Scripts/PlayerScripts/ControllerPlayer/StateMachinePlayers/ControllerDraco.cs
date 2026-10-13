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

    [Header("Tongue Walk")]
    [Tooltip("Quirino cuja lingua o Draco pode usar como ponte")]
    [SerializeField] private ControllerQuirino m_quirino;
    public ITongueBridge TongueBridge => m_quirino != null ? m_quirino.Tongue : null;

    [SerializeField] private TimedInputBuffer m_interactBuffer = new();

    public DracoMotor Motor { get; private set; }

    public bool HasMoveInput => PlayerInput.Move.sqrMagnitude >= k_inputDeadZone;

    // Estado para voltar quando sai da lingua
    public State LocomotionState => HasMoveInput ? MoveState : IdleState;

    public IdleStateDraco IdleState { get; private set; }
    public MovimentStateDraco MoveState { get; private set; }
    public JumpStateDraco JumpState { get; private set; }
    public TongueWalkStateDraco TongueWalkState { get; private set; }

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
        TongueWalkState = new TongueWalkStateDraco(this);
    }

    private void OnEnable()
    {
        m_inputReader.EnableGameplay();
        m_inputReader.JumpStarted += OnJumpStarted;
        m_inputReader.JumpCanceled += OnJumpCanceled;
        m_inputReader.Interact += OnInteract;
    }

    private void OnDisable()
    {
        m_inputReader.DisableGameplay();
        m_inputReader.JumpStarted -= OnJumpStarted;
        m_inputReader.JumpCanceled -= OnJumpCanceled;
        m_inputReader.Interact -= OnInteract;
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
        // Na lingua quem move e o proprio estado; o probe grudaria em parede/chao
        if (m_currentState != TongueWalkState)
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

    // Chamado pelos estados de locomocao: sobe na lingua se apertou Interagir perto dela
    public bool TryMountTongue()
    {
        if (!m_interactBuffer.HasBufferedInput || !TongueWalkState.CanMount()) return false;

        m_interactBuffer.Consume();
        SwitchState(TongueWalkState);
        return true;
    }

    private void OnJumpStarted() { m_jumpBuffer.Set(); JumpHeld = true; }
    private void OnJumpCanceled() { JumpHeld = false; }
    private void OnInteract() => m_interactBuffer.Set();

}
