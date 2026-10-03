using System;
using UnityEngine;

[Serializable]
public class QuirinoSettings
{
    [Header("Movement")]
    public float MoveSpeed = 5f;
    public float AccelerationPlayer = 15;
    public float DecelerationPlayer = 20;

    [Header("Jump")]
    public float JumpForce = 15;
    public float JumpCutMultiplier = 0.5f;

    [Header("Ground Check")]
    [Tooltip("Raio da esfera que procura o chao embaixo do pe")]
    public float GroundCheckRadius = 0.25f;
    [Tooltip("Distancia maxima do pe ate o chao para considerar que esta no chao")]
    public float GroundCheckDistance = 0.1f;
}
