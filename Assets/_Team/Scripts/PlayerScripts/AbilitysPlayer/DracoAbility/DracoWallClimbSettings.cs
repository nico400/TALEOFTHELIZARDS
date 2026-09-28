using System;
using UnityEngine;

[Serializable]
public class DracoWallClimbSettings
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
}
