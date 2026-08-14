using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HandlerPlayer : MonoBehaviour
{
    private enum StateInput { Keyboard, Gamepad }
    [SerializeField] private StateInput m_currentInput = StateInput.Keyboard;

    [Header("Input Controller")]
    [SerializeField] private InputReader m_inputReader;
    ChangeInputDirectionMove m_inputDirectionStrategy;
    ChangeInputJump m_inputJumpStrategy;

    [Header("Player Components")]
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private Transform m_playerCamera;

    [Header("Player Movement Config")]
    [SerializeField] private bool m_canMove = true;
    [SerializeField] private float m_maxSpeed = 5f;
    [SerializeField] private float m_acceleration = 10f;
    [SerializeField] private float m_deceleration = 10f;

    [Header("Player Gravity Config")]
    [SerializeField] private bool m_usePersoGravity = true;
    [SerializeField] private float m_gravityForce = -9.81f;
    [SerializeField] private float m_gravityMultiplier = 2f;

    [Header("Player Jump Config")]
    [SerializeField] private float m_jumpForce = 2.5f;
    [SerializeField] private float m_jumpMultiplier = 0.5f;
    [SerializeField] private bool m_isGrounded;
    [SerializeField] private LayerMask m_maskToIgnoreToCheck;

    // componentes
    private IMovimentation m_movementComponent;
    private IGravity m_gravityComponent;
    private ICheckGround m_checkGroundComponent;
    private IJump m_jumpComponent;

    private Vector3 m_directionMove;
    private Vector3 m_velocity;

    [SerializeField]  private bool m_jumpPressed;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (m_inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (m_playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);
        if (m_rigidbody == null)
            Debug.LogError("Rigidbody não foi atribuído!", this);

        if (m_inputDirectionStrategy == null)
            m_inputDirectionStrategy = new ChangeInputDirectionMove();

        if (m_inputJumpStrategy == null)
            m_inputJumpStrategy = new ChangeInputJump();

        // Habilitar entrada do jogador
        m_inputReader.EnablePlayerInput();

        // Inicializar componentes do jogador
        m_movementComponent = new Movimentation(m_maxSpeed, m_acceleration, m_deceleration);
        m_gravityComponent = new Gravity(m_gravityForce, m_gravityMultiplier);
        m_checkGroundComponent = new CheckGround();
        m_jumpComponent = new JumpPlayer(m_jumpForce, m_jumpMultiplier);

        // Inicializar estratégia de entrada com base no tipo de entrada atual
        m_inputDirectionStrategy.SetStrategyInput(m_currentInput == StateInput.Keyboard ? new GetDirectionMoveKeyboard() : new GetDirectionMoveGamepad());
        m_inputJumpStrategy.SetStrategyInput(m_currentInput == StateInput.Keyboard ? new GetButtonJumpKeyboard() : new GetButtonJumpGamepad());

        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleInputPlayer();

        float _distanceToGround = 0.5f; // Distância para verificar se o jogador está no chão
        float _offsetHeight = 0.3f; // Altura do offset para a verificação de colisão com o chão
        float _radiusSphereCheck = 0.25f; ; // Raio da esfera para a verificação de colisão
        m_isGrounded = m_checkGroundComponent.IsGroundCheck(_distanceToGround, _radiusSphereCheck, transform.position + transform.up * _offsetHeight, -transform.up, m_maskToIgnoreToCheck);

        print($"Is jump: {m_inputReader.JumpBoardInput || m_inputReader.JumpPadInput}");
    }

    private void FixedUpdate()
    {
        HandleVelocityPlayer();
        m_jumpComponent.Jump(m_rigidbody, m_isGrounded, out bool inJump, m_inputReader, m_jumpPressed);
    }

    void HandleInputPlayer()
    {
        // Atualiza a direção de movimento com base na entrada do jogador
        m_directionMove = m_inputDirectionStrategy.GetDirectionMove(m_playerCamera, m_inputReader);

        // Atualiza o estado do botão de pulo com base na entrada do jogador
        m_jumpPressed = m_inputJumpStrategy.ExecuteJump(m_inputReader);

        Debug.Log($"Jump pressed: {m_jumpPressed}");
    }

    void HandleVelocityPlayer()
    {
        // Seta a velocidade do jogador com base na direção de movimento e na velocidade atual
        m_velocity = m_movementComponent.Move(m_canMove, m_velocity, m_directionMove);

        // Nova velocidade do jogador é calculada com base na velocidade atual e na direção de movimento
        Vector3 _newVelocity = m_rigidbody.linearVelocity;
        _newVelocity.x = m_velocity.x;
        _newVelocity.z = m_velocity.z;

        // Seta a velocidade vertical do jogador com base na gravidade
        if (m_usePersoGravity)
        {
            _newVelocity = m_gravityComponent.ApplyGravity(m_isGrounded, _newVelocity);
        }

        // Aplica a velocidade calculada ao Rigidbody do jogador
        m_rigidbody.linearVelocity = _newVelocity;
    }
}
