using System;
using UnityEngine;

[Serializable]
public class QuirinoSettings
{
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float AccelerationPlayer = 15;
    public float DecelerationPlayer = 20;

    [Header("Rotation")]
    [Tooltip("Velocidade que o Quirino vira para a direcao do movimento")]
    public float RotationSpeed = 55f;

    [Header("Jump")]
    public float JumpForce = 15;
    public float JumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    [Tooltip("Raio da esfera que procura o chao embaixo do pe")]
    public float GroundCheckRadius = 0.25f;
    [Tooltip("Distancia maxima do pe ate o chao para considerar que esta no chao")]
    public float GroundCheckDistance = 0.1f;

    [Header("Jump Point")]
    [Tooltip("Raio da area que procura JumpPoints em volta do Quirino")]
    public float JumpPointDetectRadius = 6f;
    [Tooltip("Angulo maximo entre a direcao do pulo e o Target para ele valer")]
    public float JumpPointMaxAngle = 35f;
    [Tooltip("Layers dos objetos com JumpPoint")]
    public LayerMask JumpPointLayers = ~0;
    [Tooltip("Altura do arco no meio do pulo direcionado")]
    public float TargetJumpArcHeight = 1.5f;
    [Tooltip("Velocidade horizontal do pulo direcionado (define a duracao)")]
    public float TargetJumpSpeed = 8f;
    [Tooltip("Duracao minima do pulo direcionado, para alvos muito perto")]
    public float TargetJumpMinDuration = 0.35f;

    [Header("Tongue")]
    [Tooltip("Distancia maxima que a lingua alcanca a partir da boca")]
    public float TongueRange = 8f;
    public float TongueExtendSpeed = 30f;
    public float TongueRetractSpeed = 40f;
    
    [Tooltip("Tempo com a lingua presa ate disparar o evento do alvo")]
    public float TongueHeldEventTime = 1f;

    [Header("Camera")]
    [Tooltip("Velocidade que a camera troca Offset para melhor mira")]
    public float OffsetSpeedCam = 5f;
    [Tooltip("Offset padrao do CinemachineRotationComposer")]
    public Vector3 RotationComposerCam = new Vector3(0, 1.5f, 0);
    [Tooltip("Offset padrao do CinemachineOrbitalFollow")]
    public Vector3 OrbitFollowCam = new Vector3(0, 1f, 0);
    [Tooltip("Quanto a camera sobe (somado aos offsets padrao) enquanto mira")]
    public Vector3 AimCameraOffset = new Vector3(0, 0.5f, 0);
}
