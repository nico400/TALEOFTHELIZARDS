using System;
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
    public InputReader InputReader => m_inputReader;
    ChangeInputDirectionMove m_inputDirectionStrategy;

    [Header("Input Buffer")]
    [SerializeField] TimedInputBuffer m_jumpBufferStart = new();
    [SerializeField] TimedInputBuffer m_jumpBufferRealease = new();

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
    public Vector3 DirectionMove => m_directionMove;

    private Vector3 m_velocity;

    private bool m_jumpStartLatched;
    private bool m_jumpReleaseThisFixedFrame;
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

        // Habilitar entrada do jogador
        m_inputReader.EnablePlayerInput();

        // Inicializar componentes do jogador
        m_movementComponent = new Movimentation(m_maxSpeed, m_acceleration, m_deceleration);
        m_gravityComponent = new Gravity(m_gravityForce, m_gravityMultiplier);
        m_checkGroundComponent = new CheckGround();
        m_jumpComponent = new JumpPlayer(m_jumpForce, m_jumpMultiplier);

        // Inicializar estratégia de entrada com base no tipo de entrada atual
        m_inputDirectionStrategy.SetStrategyInput
            (
                m_currentInput == StateInput.Keyboard ? 
                new GetDirectionMoveKeyboard() : 
                new GetDirectionMoveGamepad()
            );
        
        m_rigidbody = GetComponent<Rigidbody>();
    }
    void OnEnable() 
    {
        // Eventos do pulo
        if (m_currentInput == StateInput.Keyboard) {
            m_inputReader.JumpBoard += OnJumpStart;
        }
        else {
            m_inputReader.JumpPad += OnJumpStart;
        }
    }

    void OnDisable() 
    {
        m_inputReader.JumpBoard -= OnJumpStart;
        m_inputReader.JumpPad -= OnJumpStart;
    }

    private void Update()
    {
        HandleInputPlayer();

        float _distanceToGround = 0.5f; // Distância para verificar se o jogador está no chão
        float _offsetHeight = 0.3f; // Altura do offset para a verificação de colisão com o chão
        float _radiusSphereCheck = 0.25f; ; // Raio da esfera para a verificação de colisão
        m_isGrounded = m_checkGroundComponent.IsGroundCheck(_distanceToGround, _radiusSphereCheck, transform.position + transform.up * _offsetHeight, -transform.up, m_maskToIgnoreToCheck);
    }

    private void FixedUpdate()
    {
        HandleVelocityPlayer();
        HandleJumpPlayer();
    }

    void HandleInputPlayer()
    {
        // Atualiza a direção de movimento com base na entrada do jogador
        m_directionMove = m_inputDirectionStrategy.GetDirectionMove(m_playerCamera, m_inputReader);
    }

    void HandleVelocityPlayer()
    {
        if (!m_canMove) return;

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
    void HandleJumpPlayer() 
    {
        bool jumpStartThisFixedFrame = m_jumpStartLatched;
        m_jumpStartLatched = false;

        bool jumpReleaseThisFixedFrame = m_jumpReleaseThisFixedFrame;
        m_jumpReleaseThisFixedFrame = false;

        m_jumpComponent.Jump(m_rigidbody, m_isGrounded, m_jumpBufferStart, m_jumpBufferRealease, jumpStartThisFixedFrame, jumpReleaseThisFixedFrame);
    }
    private void OnJumpStart(bool pressed) 
    {
        if (pressed) 
        {
            m_jumpStartLatched = true;
        }
        else 
        {
            m_jumpReleaseThisFixedFrame = true;
        }
    }
}
