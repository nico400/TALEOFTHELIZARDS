using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HandlerPlayer : MonoBehaviour
{
    enum StateInput { Keyboard, Gamepad }
    [SerializeField] StateInput currentInput = StateInput.Keyboard;

    [Header("Input Controller")]
    [SerializeField] InputReader inputReader;

    [Header("Player Components")]
    [SerializeField] CharacterController characterController;
    [SerializeField] Transform playerCamera;

    [Header("Player Movement Config")]
    [SerializeField] bool canMove = true;
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float acceleration = 10f;
    [SerializeField] float deceleration = 10f;

    [Header("Player Gravity Config")]
    [SerializeField] bool useGravity = true;
    [SerializeField] float gravityForce = -9.81f;
    [SerializeField] float gravityMultiplier = 2f;

    [Header("Player Scanner Config")]
    [SerializeField] float checkDistanceObstacle = 1f;
    [SerializeField] float checkHeightObstacle = 1f;
    [SerializeField] LayerMask obstacleLayer;

    [Header("Player Jump Config")]
    [SerializeField] float jumpHeight = 2.5f;
    [SerializeField] float jumpDuration = 1.25f;

    IMovimentation movementComponent;
    IGravity gravityComponent;
    IScannerEnvironment scannerComponent;
    IJump jumpComponent;

    Vector3 DirectionMove;
    Vector3 Velocity;
    Vector3 ObstacleEdgePosition;
    Vector3 PosInFrontToEdge;
    bool interactInput;
    bool canJump;
    bool isJumping;
    bool aimInput;
    bool fireTongueInput;

    private void Awake()
    {
        if (inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);

        // Habilitar entrada do jogador
        inputReader.EnablePlayerInput();

        // Inicializar componentes do jogador
        movementComponent = new Movimentation(maxSpeed, acceleration, deceleration);
        gravityComponent = new Gravity(gravityForce, gravityMultiplier);
        scannerComponent = new ScannerEnvieronment(checkDistanceObstacle, checkHeightObstacle);
        jumpComponent = new JumpPlayer(jumpDuration, jumpHeight);

        switch (currentInput)
        {
            case StateInput.Keyboard:
                InitAbilitysKeyboard();
                break;
            case StateInput.Gamepad:
                InitAbilitysGamepad();
                break;
        }

        characterController = GetComponent<CharacterController>();
    }

    // Usar Update para lidar com a entrada do jogador,
    // varredura de obstáculos e ações de pulo
    private void Update()
    {
        HandleAllInputs();
        HandleScanner();
        HandleJump();
    }

    // Usar FixedUpdate para lidar com o movimento e a gravidade do jogador,
    // garantindo uma atualização consistente
    private void FixedUpdate()
    {
        if (isJumping) return; // Pular o tratamento de movimento e gravidade durante o pulo
        HandleMoviment();
    }

    private void InitAbilitysKeyboard()
    {
        
    }

    private void InitAbilitysGamepad()
    {
        
    }
    // Lidar com a entrada do jogador com base no método de entrada atual (teclado ou gamepad)
    void HandleAllInputs()
    {     
        switch (currentInput)
        {
            case StateInput.Keyboard:
                HandleKeyboardInput();
                break;
            case StateInput.Gamepad:
                HandleGamepadInput();
                break;
        }
    }
    // Lidar com a ação de pulo do jogador
    void HandleJump()
    {
        // Se o jogador puder pular e a entrada de interação for detectada, realizar a ação de pulo
        if (interactInput && canJump)
        {
            jumpComponent.JumpInitialize(ObstacleEdgePosition, PosInFrontToEdge, isJumping);
            isJumping = true;            
        }
        bool jumpingInProgress = jumpComponent.Jump(characterController, isJumping);
        if (!jumpingInProgress)
            isJumping = false;
    }
    void HandleMoviment()
    {
        // Lidar com o movimento e gravidade do jogador
        Vector2 inputDirection = new Vector2(DirectionMove.x, DirectionMove.z);

        // Mover o jogador com base na entrada e aplicar gravidade
        Velocity = movementComponent.Move(canMove, Velocity, inputDirection);
        Velocity = gravityComponent.ApplyGravity(characterController.isGrounded, Velocity);

        // Mover o jogador usando o CharacterController
        characterController.Move(Velocity * Time.deltaTime);
    }
    // Lidar com a varredura de obstáculos na direção
    // do movimento e a altura dos obstáculos à frente do jogador
    void HandleScanner()
    {
        // Scanear obstáculos na direção do movimento
        Vector3 camLookDir = playerCamera.forward;
        camLookDir.y = 0f;
        camLookDir = camLookDir.normalized;

        // Scanear obstáculos na direção do movimento
        bool obstacleInDirection = scannerComponent.ScanObstacleInDirection(transform.position, camLookDir,
            obstacleLayer, out RaycastHit hit);

        // Scanear a altura do obstáculo à frente do jogador
        //Vector3 obstacleRayPos = transform.position + (camLookDir * checkDistanceObstacle) + (Vector3.up * checkHeightObstacle);
        Vector3 obstacleRayPos = hit.point + (Vector3.up * checkHeightObstacle);
        bool obstacleHeight = scannerComponent.ScanObstacleHeight(obstacleRayPos, Vector3.down,
            obstacleLayer, out RaycastHit hitHeight);

        // Se houver um obstáculo na direção do movimento e sua altura for compatível, posso interagir com ele para pular
        if (obstacleInDirection && obstacleHeight)
        {
            ObstacleEdgePosition = new Vector3(hit.point.x, hitHeight.point.y, hit.point.z);
            PosInFrontToEdge = hit.point + (hit.normal * 1.45f);
            canJump = true;
        }
        else
            canJump = false;
    }
    // Lidar com a entrada do teclado para o movimento do jogador
    void HandleKeyboardInput()
    {
        // Calcular a direção do movimento com base nos vetores forward e right da câmera
        DirectionMove = playerCamera.forward * inputReader.DirectionBoard.y +
            playerCamera.right * inputReader.DirectionBoard.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;

        // Verificar a entrada de interação do teclado
        interactInput = inputReader.InteractBoardInput;
        aimInput = inputReader.AimBoardInput;
        fireTongueInput = inputReader.FireTongueInput;
    }
    // Lidar com a entrada do gamepad para o movimento do jogador
    private void HandleGamepadInput()
    {
        // Calcular a direção do movimento com base nos vetores forward e right da câmera
        DirectionMove = playerCamera.forward * inputReader.DirectionPad.y +
            playerCamera.right * inputReader.DirectionPad.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;

        // Verificar a entrada de interação do gamepad
        interactInput = inputReader.InteractPadInput;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(ObstacleEdgePosition, 0.1f);
        Gizmos.DrawSphere(PosInFrontToEdge, 0.1f);
    }
}
