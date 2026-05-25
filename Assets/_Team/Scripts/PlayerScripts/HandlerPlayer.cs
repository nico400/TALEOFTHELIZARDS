using System;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Playables;

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

    [Header("Player Togue Abilit Config")]
    [SerializeField] float tongueRange = 5f;
    [SerializeField] float radiusSphereCast = 0.5f;
    [SerializeField] float tongueSpeed = 10f;
    [SerializeField] string tongueAttachTag = "TongueAttach";
    [SerializeField] LayerMask tongueAttachLayer;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] Transform startTogue;
    [SerializeField] CinemachineCameraOffset aimOffsetCam;

    // componentes
    IMovimentation movementComponent;
    IGravity gravityComponent;
    IScannerEnvironment scannerComponent;
    IJump jumpComponent;

    // habilidades específicas (exemplo: ataque com língua)
    IGetPositionTongueFinal getPositionTongueFinal;
    ITongueAttach tongueAttachComponent;

    Vector3 DirectionMove;
    Vector3 Velocity;
    Vector3 ObstacleEdgePosition;
    Vector3 PosInFrontToEdge;
    Vector3 PosTogueFinal;

    bool interactInput;
    bool canJump;
    bool isJumping;
    bool aimInput;
    bool fireTongueInput;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

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
            // Inicializar componentes das habilidades específicas com base no método de entrada selecionado (teclado ou gamepad)
            case StateInput.Keyboard:
                InitAbilitysComponentsKeyboard();
                break;
            case StateInput.Gamepad:
                InitAbilitysComponentsGamepad();
                break;
        }

        characterController = GetComponent<CharacterController>();
    }

    // Usar Update para lidar com a entrada do jogador, varredura de obstáculos, ação de pulo e habilidades, garantindo uma resposta rápida às ações do jogador
    private void Update()
    {
        HandleAllInputs();
        HandleScanner();
        HandleJump();
        HandleAbilitys();
    }

    // Usar FixedUpdate para lidar com o movimento e a gravidade do jogador, garantindo uma atualização consistente
    private void FixedUpdate()
    {
        if (isJumping) return; // Pular o tratamento de movimento e gravidade durante o pulo
        HandleMoviment();
    }

    // Inicializar habilidades específicas para o teclado, como mira e ataque com língua
    private void InitAbilitysComponentsKeyboard()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (aimOffsetCam == null)
            aimOffsetCam = GetComponentInChildren<CinemachineCameraOffset>();

        getPositionTongueFinal = new GetPositionTongueFinal(tongueRange, radiusSphereCast, tongueAttachTag, tongueAttachLayer, obstacleLayer);
        tongueAttachComponent = new TongueAttach();
    }

    private void InitAbilitysComponentsGamepad()
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

    // Lidar com o movimento e a gravidade do jogador usando os componentes de movimentação e gravidade
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

    // Lidar com as habilidades específicas do jogador, como mira e ataque com língua, com base na entrada do jogador
    private void HandleAbilitys()
    {
        switch (currentInput)
        {
            case StateInput.Keyboard:
                UseAbilityKeyboard();
                break;
            case StateInput.Gamepad:
                UseAbilityGamepad();
                break;
        }
    }

    // Implementar a lógica para usar as habilidades específicas do jogador de gamepad, como a escalada e subir em cima da língua, com base na entrada do jogador
    private void UseAbilityGamepad()
    {

    }

    // Implementar a lógica para usar as habilidades específicas do jogador de teclado, como mira e ataque com língua, com base na entrada do jogador
    private void UseAbilityKeyboard()
    {       
        if (getPositionTongueFinal != null && PosTogueFinal == Vector3.zero)
            PosTogueFinal = getPositionTongueFinal.PositionFinalTongue(playerCamera.position + (playerCamera.forward * 2), playerCamera.forward);

        // Lógica para ataque com língua
        if (aimInput)
        {
            aimOffsetCam.Offset = new Vector3(1f, 0f, 0.75f);
            
            if (fireTongueInput && PosTogueFinal != Vector3.zero)
                if (tongueAttachComponent != null)
                    tongueAttachComponent.TongueAttachActive(lineRenderer, startTogue.position, PosTogueFinal);
            
        }else
            aimOffsetCam.Offset = Vector3.zero;

        if (!fireTongueInput || !aimInput)
        {
            tongueAttachComponent.TongueAttachDeactive(lineRenderer);
            PosTogueFinal = Vector3.zero;
        }
    }

    // Lidar com a varredura de obstáculos na direção do movimento e a altura dos obstáculos à frente do jogador
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

        // Verificar a entrada de mira com língua do teclado
        aimInput = inputReader.AimBoardInput;

        // Verificar a entrada de ataque com língua do teclado 
        if (aimInput)
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

        if (currentInput == StateInput.Keyboard)
        {
            Vector3 origin = playerCamera.position;
            Vector3 targetPosition = origin + (playerCamera.forward * tongueRange);
            DrawWireCapsule(origin, targetPosition, radiusSphereCast);
            Gizmos.DrawSphere(PosTogueFinal, 0.1f);
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
