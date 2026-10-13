using System;
using UnityEngine;

[Serializable]
public class DracoSettings
{
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float AccelerationPlayer = 15;
    public float DecelerationPlayer = 20;

    [Header("Jump")]
    public float JumpForce = 15;
    public float JumpCutMultiplier = 0.5f;
    [Tooltip("Tempo que o Draco ignora a parede depois de pular dela")]
    public float WallJumpDetachTime = 0.2f;

    [Header("Ground Check")]
    [Tooltip("Distancia maxima do pe ate o chao para considerar que esta no chao")]
    public float GroundedDistance = 0.2f;

    [Header("Rotation")]
    public float NormalSlerpSpeed = 55f;
    public float NormalGraceTime = 0.35f;

    [Header("Ground Snap")]
    public float SnapDistanceMin = 0.095f;
    public float SnapForce = 4f;

    [Header("Surface Probe")]
    public float GroundProbeRadius = 0.75f;
    public float WallProbeRadius = 0.2f;
    public float GroundProbeDistance = 2.35f;
    public float WallProbeDistance = 1.35f;

    [Header("Tongue Walk")]
    [Tooltip("Distancia maxima do Draco ate a lingua para conseguir subir")]
    public float TongueMountDistance = 1.5f;
    public float TongueWalkSpeed = 3f;
    [Tooltip("Distancia que o Draco para antes da boca e da ponta da lingua")]
    public float TongueEdgeMargin = 0.5f;
    [Tooltip("Altura do pe do Draco acima da linha da lingua")]
    public float TongueStandHeight = 0.05f;
}
