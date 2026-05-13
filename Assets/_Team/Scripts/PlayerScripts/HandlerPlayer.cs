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
    [SerializeField] float gravityForce = -9.81f;
    [SerializeField] float gravityMultiplier = 2f;

    [Header("Player Scanner Config")]
    [SerializeField] float checkDistanceObstacle = 1f;
    [SerializeField] float checkHeightObstacle = 1f;
    [SerializeField] LayerMask obstacleLayer;

    IMovimentation movementComponent;
    IGravity gravityComponent;
    IScannerEnvironment scannerComponent;

    Vector3 DirectionMove;
    Vector3 Velocity;
    Vector3 LastInputDirection;
    Vector3 ObstacleEdgePosition;
    bool interactInput;
    bool canJump;

    private void Awake()
    {
        if (inputReader == null)
            Debug.LogError("InputReader não foi atribuído!", this);
        if (playerCamera == null)
            Debug.LogError("Player Camera não foi atribuída!", this);

        // Enable player input
        // Habilitar entrada do jogador
        inputReader.EnablePlayerInput();

        // Initialize player components
        // Inicializar componentes do jogador
        movementComponent = new Movimentation(maxSpeed, acceleration, deceleration);
        gravityComponent = new Gravity(gravityForce, gravityMultiplier);
        scannerComponent = new ScannerEnvieronment(checkDistanceObstacle, checkHeightObstacle);
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Handle player input based on the current input method (keyboard or gamepad)
        // Lidar com a entrada do jogador com base no método de entrada atual (teclado ou gamepad)
        switch (currentInput)
        {
            // Lidar com a entrada do jogador com base no método de entrada atual (teclado ou gamepad) // 
            // Handle player input based on the current input method (keyboard or gamepad) // 
            case StateInput.Keyboard:
                HandleKeyboardInput();
                break;
            case StateInput.Gamepad:
                HandleGamepadInput();
                break;
        }

        HandleScanner();
    }


    private void FixedUpdate()
    {
        // Handle player movement and gravity
        // Lidar com o movimento e gravidade do jogador
        Vector2 inputDirection = new Vector2(DirectionMove.x, DirectionMove.z);

        // Move the player based on input and apply gravity
        // Mover o jogador com base na entrada e aplicar gravidade
        Velocity = movementComponent.Move(canMove, Velocity, inputDirection);
        Velocity = gravityComponent.ApplyGravity(characterController.isGrounded, Velocity);

        // Move the player using the CharacterController
        // Mover o jogador usando o CharacterController
        characterController.Move(Velocity * Time.deltaTime);
    }

    // Handle scanning for obstacles in the direction
    // of movement and the height of obstacles in front of the player
    // Lidar com a varredura de obstáculos na direção
    // do movimento e a altura dos obstáculos à frente do jogador
    private void HandleScanner()
    {
        // Scan obstacles in the direction of movement
        // Scanear obstáculos na direção do movimento
        Vector3 camLookDir = playerCamera.forward;
        camLookDir.y = 0f;
        camLookDir = camLookDir.normalized;

        // Scan for obstacles in the direction of movement
        // Scanear obstáculos na direção do movimento
        bool obstacleInDirection = scannerComponent.ScanObstacleInDirection(transform.position, camLookDir,
            obstacleLayer, out RaycastHit hit);

        // Scan the height of the obstacle in front of the player
        // Scanear a altura do obstáculo à frente do jogador
        //Vector3 obstacleRayPos = transform.position + (camLookDir * checkDistanceObstacle) + (Vector3.up * checkHeightObstacle);
        Vector3 obstacleRayPos = hit.point + (Vector3.up * checkHeightObstacle);
        bool obstacleHeight = scannerComponent.ScanObstacleHeight(obstacleRayPos, Vector3.down,
            obstacleLayer, out RaycastHit hitHeight);

        // If there is an obstacle in the direction of movement and its height is compatible, i can interact with it to jump
        // Se houver um obstáculo na direção do movimento e sua altura for compatível, posso interagir com ele para pular
        if (obstacleInDirection && obstacleHeight)
        {
            ObstacleEdgePosition = new Vector3(hit.point.x, hitHeight.point.y, hit.point.z);
            canJump = true;
        }
        else
            canJump = false;
    }

    // Handle keyboard input for player movement
    // Lidar com a entrada do teclado para o movimento do jogador
    private void HandleKeyboardInput()
    {
        // Calculate the movement direction based on the camera's forward and right vectors
        // Calcular a direção do movimento com base nos vetores forward e right da câmera
        DirectionMove = playerCamera.forward * inputReader.DirectionBoard.y +
            playerCamera.right * inputReader.DirectionBoard.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;

        // Check for interaction input from the keyboard
        // Verificar a entrada de interação do teclado
        interactInput = inputReader.InteractBoardInput;
    }

    // Handle gamepad input for player movement
    // Lidar com a entrada do gamepad para o movimento do jogador
    private void HandleGamepadInput()
    {
        // Calculate the movement direction based on the camera's forward and right vectors
        // Calcular a direção do movimento com base nos vetores forward e right da câmera
        DirectionMove = playerCamera.forward * inputReader.DirectionPad.y +
            playerCamera.right * inputReader.DirectionPad.x;
        DirectionMove.y = 0f;
        DirectionMove = DirectionMove.normalized;

        // Check for interaction input from the gamepad
        // Verificar a entrada de interação do gamepad
        interactInput = inputReader.InteractPadInput;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(ObstacleEdgePosition, 0.1f);
    }
}
