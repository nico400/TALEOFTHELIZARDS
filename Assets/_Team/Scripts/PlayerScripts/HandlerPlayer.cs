using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HandlerPlayer : MonoBehaviour
{
    private enum StateInput { Keyboard, Gamepad }
    [SerializeField] private StateInput m_currentInput = StateInput.Keyboard;

    [Header("Input Controller")]
    [SerializeField] private InputReader m_inputReader;

    [Header("Player Components")]
    [SerializeField] private CharacterController m_characterController;
    [SerializeField] private Transform m_playerCamera;

    [Header("Player Movement Config")]
    [SerializeField] private bool m_canMove = true;
    [SerializeField] private float m_maxSpeed = 5f;
    [SerializeField] private float m_acceleration = 10f;
    [SerializeField] private float m_deceleration = 10f;

    [Header("Player Gravity Config")]
    [SerializeField] private bool m_useGravity = true;
    [SerializeField] private float m_gravityForce = -9.81f;
    [SerializeField] private float m_gravityMultiplier = 2f;

    [Header("Player Scanner Config")]
    [SerializeField] private float m_checkDistanceObstacle = 1f;
    [SerializeField] private float m_checkHeightObstacle = 1f;
    [SerializeField] private LayerMask m_obstacleLayer;

    [Header("Player Jump Config")]
    [SerializeField] private float m_jumpHeight = 2.5f;
    [SerializeField] private float m_jumpDuration = 1.25f;

    [Header("Player Togue Abilit Config")]
    [SerializeField] private float m_tongueRange = 5f;
    [SerializeField] private float m_radiusSphereCast = 0.5f;
    [SerializeField] private float m_tongueSpeed = 10f;
    [SerializeField] private string m_tongueAttachTag = "TongueAttach";
    [SerializeField] private LayerMask m_tongueAttachLayer;
    [SerializeField] private LineRenderer m_lineRenderer;
    [SerializeField] private Transform m_startTogue;
    [SerializeField] private CinemachineCameraOffset m_aimOffsetCam;

    // componentes
    private IMovimentation m_movementComponent;
    private IGravity m_gravityComponent;
    private IScannerEnvironment m_scannerComponent;
    private IJump m_jumpComponent;

    // habilidades específicas (exemplo: ataque com língua)
    private IGetPositionTongueFinal m_getPositionTongueFinal;
    private ITongueAttach m_tongueAttachComponent;

    private Vector3 m_directionMove;
    private Vector3 m_velocity;
    private Vector3 m_obstacleEdgePosition;
    private Vector3 m_posInFrontToEdge;
    private Vector3 m_posTogueFinal;

    private bool m_interactInput;
    private bool m_canJump;
    private bool m_isJumping;

    private IPlayerInputStrategy m_inputStrategy;
     
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (m_inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (m_playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);

        // Habilitar entrada do jogador
        m_inputReader.EnablePlayerInput();

        // Inicializar componentes do jogador
        m_movementComponent = new Movimentation(m_maxSpeed, m_acceleration, m_deceleration);
        m_gravityComponent = new Gravity(m_gravityForce, m_gravityMultiplier);
        m_scannerComponent = new ScannerEnvieronment(m_checkDistanceObstacle, m_checkHeightObstacle);
        m_jumpComponent = new JumpPlayer(m_jumpDuration, m_jumpHeight);

        m_inputStrategy = m_currentInput == StateInput.Keyboard ?
            new KeyboardInputStrategy() : new GamepadInputStrategy();

        m_characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        m_directionMove = m_inputStrategy.GetDirectionMove(m_playerCamera, m_inputReader);
        m_interactInput = m_currentInput == StateInput.Keyboard ? m_inputReader.InteractBoardInput : m_inputReader.InteractPadInput;

        HandleScanner();
        HandleJump();
        
        print($"Can Jump: {m_canJump}, Is Jumping: {m_isJumping}");
    }

    private void FixedUpdate()
    {
        if (m_isJumping) return; 
        HandleMoviment();
    }

    void HandleJump()
    {
        if (m_interactInput && m_canJump)
        {
            print("Jump!");
            m_jumpComponent.JumpInitialize(m_obstacleEdgePosition, m_posInFrontToEdge, m_isJumping);
            m_isJumping = true;            
        }
        bool jumpingInProgress = m_jumpComponent.Jump(m_characterController, m_isJumping);
        if (!jumpingInProgress)
            m_isJumping = false;
    }
    void HandleMoviment()
    {
        // Lidar com o movimento e gravidade do jogador
        Vector2 inputDirection = new Vector2(m_directionMove.x, m_directionMove.z);

        // Mover o jogador com base na entrada e aplicar gravidade
        m_velocity = m_movementComponent.Move(m_canMove, m_velocity, inputDirection);
        m_velocity = m_gravityComponent.ApplyGravity(m_characterController.isGrounded, m_velocity);

        // Mover o jogador usando o CharacterController
        m_characterController.Move(m_velocity * Time.deltaTime);
    }
    void HandleScanner()
    {
        // Scanear obstáculos na direção do movimento
        Vector3 camLookDir = m_playerCamera.forward;
        camLookDir.y = 0f;
        camLookDir = camLookDir.normalized;

        // Scanear obstáculos na direção do movimento
        bool obstacleInDirection = m_scannerComponent.ScanObstacleInDirection(transform.position, camLookDir,
            m_obstacleLayer, out RaycastHit hit);

        // Scanear a altura do obstáculo à frente do jogador
        //Vector3 obstacleRayPos = transform.position + (camLookDir * checkDistanceObstacle) + (Vector3.up * checkHeightObstacle);
        Vector3 obstacleRayPos = hit.point + (Vector3.up * m_checkHeightObstacle);
        bool obstacleHeight = m_scannerComponent.ScanObstacleHeight(obstacleRayPos, Vector3.down,
            m_obstacleLayer, out RaycastHit hitHeight);

        // Se houver um obstáculo na direção do movimento e sua altura for compatível, posso interagir com ele para pular
        if (obstacleInDirection && obstacleHeight)
        {
            m_obstacleEdgePosition = new Vector3(hit.point.x, hitHeight.point.y, hit.point.z);
            m_posInFrontToEdge = hit.point + (hit.normal * 1.45f);
            m_canJump = true;
        }
        else
            m_canJump = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(m_obstacleEdgePosition, 0.1f);
        Gizmos.DrawSphere(m_posInFrontToEdge, 0.1f);

        if (m_currentInput == StateInput.Keyboard)
        {
            Vector3 origin = m_playerCamera.position;
            Vector3 targetPosition = origin + (m_playerCamera.forward * m_tongueRange);
            DrawWireCapsule(origin, targetPosition, m_radiusSphereCast);
            Gizmos.DrawSphere(m_posTogueFinal, 0.1f);
        }         
    }
    public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
    {
        Vector3 upOffset = point2 - point1;
        Vector3 up = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
        Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
        Vector3 forward = orientation * Vector3.forward;
        Vector3 right = orientation * Vector3.right;
        // z axis
        Handles.DrawWireArc(point2, forward, right, 180, radius);
        Handles.DrawWireArc(point1, forward, right, -180, radius);
        Handles.DrawLine(point1 + right * radius, point2 + right * radius);
        Handles.DrawLine(point1 - right * radius, point2 - right * radius);
        // x axis
        Handles.DrawWireArc(point2, right, forward, -180, radius);
        Handles.DrawWireArc(point1, right, forward, 180, radius);
        Handles.DrawLine(point1 + forward * radius, point2 + forward * radius);
        Handles.DrawLine(point1 - forward * radius, point2 - forward * radius);
        // y axis
        Handles.DrawWireDisc(point2, up, radius);
        Handles.DrawWireDisc(point1, up, radius);
    }
}
