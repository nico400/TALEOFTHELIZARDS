using System;
using UnityEngine;

[Serializable]
public class DracoWallClimbSettings
{
    [Header("Movement")]
    public float MoveSpeed = 4f;

    [Header("Rotation")]
    public float NormalSlerpSpeed = 12f;
    public float NormalGraceTime = 0.35f;

    [Header("Ground Snap")]
    public float SnapDistanceMin = 0.085f;
    public float SnapForce = 6f;

    [Header("Surface Probe")]
    public float GroundProbeRadius = 0.75f;
    public float WallProbeRadius = 0.2f;
    public float GroundProbeDistance = 2.35f;
    public float WallProbeDistance = 1.35f;
}
