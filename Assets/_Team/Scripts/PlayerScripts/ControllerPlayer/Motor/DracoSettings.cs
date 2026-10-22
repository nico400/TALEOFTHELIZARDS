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

    [Header("Surface Gravity")]
    [Tooltip("Aceleracao que puxa o Draco contra a superficie escalavel (gravidade falsa ao longo da normal)")]
    public float SurfaceGravity = 25f;
    [Tooltip("Abaixo desta distancia (casca do collider ate a superficie) a gravidade falsa desliga")]
    public float GravityOffDistance = 0.05f;

    [Header("Surface Probe")]
    [Tooltip("Raio do SphereCast que sai do centro do collider para o forward do corpo")]
    public float ForwardProbeRadius = 0.5f;
    [Tooltip("Comprimento do cast para frente, medido do centro do collider")]
    public float ForwardProbeLength = 0.8f;
    [Tooltip("Raio do SphereCast que sai do centro do collider para o -up do corpo. Menor que o raio do collider")]
    public float DownProbeRadius = 0.5f;
    [Tooltip("Comprimento do cast para baixo, medido do centro do collider")]
    public float DownProbeLength = 0.6f;

    [Header("Tongue Walk")]
    [Tooltip("Distancia maxima do Draco ate a lingua para conseguir subir")]
    public float TongueMountDistance = 1.5f;
    public float TongueWalkSpeed = 3f;
    [Tooltip("Distancia que o Draco para antes da boca e da ponta da lingua")]
    public float TongueEdgeMargin = 0.5f;
    [Tooltip("Altura do pe do Draco acima da linha da lingua")]
    public float TongueStandHeight = 0.05f;

    [Header("Grab")]
    [Tooltip("Raio em volta da boca do Draco para achar objetos pegaveis")]
    public float GrabRadius = 1f;
    [Tooltip("Layers onde ficam os objetos pegaveis")]
    public LayerMask GrabLayers = ~0;
}
