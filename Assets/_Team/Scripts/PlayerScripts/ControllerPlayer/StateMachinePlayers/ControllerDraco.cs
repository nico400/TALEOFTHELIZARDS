using UnityEngine;

public class ControllerDraco : MonoBehaviour
{
    private const float k_inputDeadZone = 0.1f;

    [SerializeField] private InputReader m_inputReader;
    public IPlayerInput PlayerInput => m_inputReader;

    [SerializeField] private Rigidbody m_rigidbody;
    public Rigidbody RB => m_rigidbody;

    [Tooltip("Collider do corpo: os casts de parede/chao saem do centro dele")]
    [SerializeField] private CapsuleCollider m_collider;

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

    [Header("Grab")]
    [Tooltip("GameObject vazio na boca do Draco: onde o objeto pego fica")]
    [SerializeField] private Transform m_holdPoint;

    public DracoMotor Motor { get; private set; }
    public DracoGrabber Grabber { get; private set; }
    public PlayerInteractor Interactor { get; private set; }
    private PlayerRespawn m_respawn;

    public bool HasMoveInput => PlayerInput.Move.sqrMagnitude >= k_inputDeadZone;

    // Estado para voltar quando sai da lingua, pousa do pulo ou solta da parede
    public State LocomotionState
    {
        get
        {
            if (Motor.IsOnClimbSurface) return ClimbState;
            return HasMoveInput ? MoveState : IdleState;
        }
    }

    public IdleStateDraco IdleState { get; private set; }
    public MovimentStateDraco MoveState { get; private set; }
    public ClimbStateDraco ClimbState { get; private set; }
    public JumpStateDraco JumpState { get; private set; }
    public TongueWalkStateDraco TongueWalkState { get; private set; }

    private State m_currentState;

    private void Awake()
    {
        if (m_rigidbody == null)
            m_rigidbody = GetComponent<Rigidbody>();

        if (m_collider == null)
            m_collider = GetComponent<CapsuleCollider>();

        if (m_settingsClimb == null)
            m_settingsClimb = new DracoSettings();

        Motor = new DracoMotor(transform, m_rigidbody, m_collider, m_camera, m_settingsClimb);

        if (m_holdPoint == null)
        {
            Debug.LogWarning($"{name}: Hold Point nao configurado, usando o proprio Draco.", this);
            m_holdPoint = transform;
        }
        Grabber = new DracoGrabber(m_holdPoint, m_settingsClimb);
        // Com objeto na boca nao usa outros interagiveis
        Interactor = new PlayerInteractor(InteractorCharacter.Draco, transform, m_holdPoint, () => !Grabber.IsHolding);
        m_respawn = GetComponent<PlayerRespawn>();

        // cria os estados uma vez aqui para nao perder os paremetros
        IdleState = new IdleStateDraco(this);
        MoveState = new MovimentStateDraco(this);
        ClimbState = new ClimbStateDraco(this);
        JumpState = new JumpStateDraco(this);
        TongueWalkState = new TongueWalkStateDraco(this);
    }

    private void OnEnable()
    {
        m_inputReader.EnableGameplay();
        m_inputReader.JumpStarted += OnJumpStarted;
        m_inputReader.JumpCanceled += OnJumpCanceled;
        m_inputReader.Interact += OnInteract;
        if (m_respawn != null)
        {
            m_respawn.Died += ResetForRespawn;
            m_respawn.Respawned += ResetForRespawn;
        }
    }

    private void OnDisable()
    {
        m_inputReader.DisableGameplay();
        m_inputReader.JumpStarted -= OnJumpStarted;
        m_inputReader.JumpCanceled -= OnJumpCanceled;
        m_inputReader.Interact -= OnInteract;
        if (m_respawn != null)
        {
            m_respawn.Died -= ResetForRespawn;
            m_respawn.Respawned -= ResetForRespawn;
        }

        // Nao deixa o objeto preso na boca de um Draco desativado
        Grabber?.Release(Vector3.zero);
        Interactor?.StopInteracting();
    }

    private void Start()
    {
        SwitchState(IdleState); // começa no Idle
    }

    private bool IsDead => m_respawn != null && m_respawn.IsDead;

    private void Update()
    {
        if (IsDead) return;

        Interactor.Update();
        m_currentState?.Update();
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

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

    // Chamado pelos estados de locomocao. Prioridade do Interagir:
    // 1. segurando algo: solta
    // 2. interagindo com algo: para
    // 3. objeto pegavel perto: pega
    // 4. interagivel perto: comeca a interagir
    // 5. lingua perto: sobe nela
    // Retorna true so quando trocou de estado (subiu na lingua)
    public bool TryInteract()
    {
        if (!m_interactBuffer.HasBufferedInput) return false;

        if (Grabber.IsHolding)
        {
            m_interactBuffer.Consume();
            Grabber.Release(m_rigidbody.linearVelocity);
            return false;
        }

        if (Interactor.IsInteracting)
        {
            m_interactBuffer.Consume();
            Interactor.StopInteracting();
            return false;
        }

        if (Grabber.TryGrabNearest() || Interactor.TryStartInteraction())
        {
            m_interactBuffer.Consume();
            return false;
        }

        if (!TongueWalkState.CanMount()) return false;

        m_interactBuffer.Consume();
        SwitchState(TongueWalkState);
        return true;
    }

    // Morreu ou renasceu: solta o que tinha na boca, desgruda da parede e volta para o Idle
    private void ResetForRespawn()
    {
        SwitchState(IdleState);
        Grabber.Release(Vector3.zero);
        Interactor.StopInteracting();
        Motor.ClearSurface();
        RB.useGravity = true;
        m_jumpBuffer.Consume();
    }

    private void OnJumpStarted() { m_jumpBuffer.Set(); JumpHeld = true; }
    private void OnJumpCanceled() { JumpHeld = false; }
    private void OnInteract() => m_interactBuffer.Set();

    // Debug do alcance dos SphereCasts de parede (ciano) e chao (amarelo)
    private void OnDrawGizmosSelected()
    {
        if (Motor != null)
        {
            Motor.DrawProbeGizmos();
            return;
        }

        // Fora do play: previa com os valores atuais do settings
        if (m_settingsClimb != null)
            new WallAndGroundSurfaceProbe(m_settingsClimb, m_collider != null ? m_collider : GetComponent<CapsuleCollider>()).DrawGizmos(transform);
    }

}
