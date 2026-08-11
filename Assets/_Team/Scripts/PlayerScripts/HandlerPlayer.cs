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
    ChangeInputDirectionMove m_inputStrategy;

    [Header("Player Components")]
    [SerializeField] private Rigidbody m_rigidbody;
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

    [Header("Player Jump Config")]
    [SerializeField] private float m_jumpHeight = 2.5f;
    [SerializeField] private bool m_isGrounded;

    // componentes
    private IMovimentation m_movementComponent;
    private IGravity m_gravityComponent;
    private IJump m_jumpComponent;

    private Vector3 m_directionMove;
    private Vector3 m_velocity;

    private bool m_canJump;
    private bool m_isJumping;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (m_inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (m_playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);
        if (m_rigidbody == null)
            Debug.LogError("Rigidbody não foi atribuído!", this);

        if (m_inputStrategy == null)
            m_inputStrategy = new ChangeInputDirectionMove();

        // Habilitar entrada do jogador
        m_inputReader.EnablePlayerInput();

        // Inicializar componentes do jogador
        m_movementComponent = new Movimentation(m_maxSpeed, m_acceleration, m_deceleration);
        m_gravityComponent = new Gravity(m_gravityForce, m_gravityMultiplier);

        // Inicializar estratégia de entrada com base no tipo de entrada atual
        m_inputStrategy.SetStrategyInput(m_currentInput == StateInput.Keyboard ? new GetDirectionMoveKeyboard() : new GetDirectionMoveGamepad());

        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        m_directionMove = m_inputStrategy.GetDirectionMove(m_playerCamera, m_inputReader);
        Debug.DrawRay(transform.position, m_directionMove * 2f, Color.green);
    }

    private void FixedUpdate()
    {
        HandleMoviment();
    }

    void HandleMoviment()
    {
        // Seta a velocidade do jogador com base na direção de movimento e na velocidade atual
        m_velocity = m_movementComponent.Move(m_canMove, m_velocity, m_directionMove);

        // Nova velocidade do jogador é calculada com base na velocidade atual e na direção de movimento
        Vector3 newVelocity = m_rigidbody.linearVelocity;
        newVelocity.x = m_velocity.x;
        newVelocity.z = m_velocity.z;

        // Aplica a velocidade calculada ao Rigidbody do jogador
        m_rigidbody.linearVelocity = newVelocity;
    }
}
