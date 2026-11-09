using System;
using UnityEngine;

// Versao antiga da escalada (usada pelo HandlerAbilitys, hoje comentado).
// O Draco atual usa o DracoMotor + ClimbStateDraco.
public class DracoWalkInWallsAbility : IAbilityPlayer
{
    private readonly Func<Vector2> m_getRawInput;
    private readonly Transform m_cam;
    private readonly Rigidbody m_rb;

    private readonly DracoSettings m_settings;
    private readonly WallAndGroundSurfaceProbe m_surfaceProbe;
    private  SurfaceNormalTracker m_normalTracker;

    private Vector3 m_lastProbeDirection = Vector3.forward;

    public DracoWalkInWallsAbility(Func<Vector2> rawInput, Transform cam, Rigidbody rb, DracoSettings settings = null)
    {
        m_getRawInput = rawInput;
        m_cam = cam;
        m_rb = rb;
        m_settings = settings ?? new DracoSettings();
        m_surfaceProbe = new WallAndGroundSurfaceProbe(m_settings, rb != null ? rb.GetComponent<CapsuleCollider>() : null);
        m_normalTracker = new SurfaceNormalTracker(m_settings.NormalGraceTime);
    }

    public void ExecuteAbilityUpdate(Transform playerTransform)
    {
        MoveThroughWall(playerTransform);
    }

    private void MoveThroughWall(Transform playerTransform)
    {
        Vector3 _normalToUse = m_normalTracker.HasNormal ? m_normalTracker.CurrentNormal : playerTransform.up;
        Vector3 _dirMoviment = ComputeMoveDirection(playerTransform, _normalToUse);

        SurfaceProbeResult _probeResult = m_surfaceProbe.Probe(playerTransform);

        m_normalTracker.Update(_probeResult, Time.deltaTime);

        RotateDraco(playerTransform, m_normalTracker.CurrentNormal);

        m_rb.linearVelocity = _dirMoviment * m_settings.MoveSpeed;
    }

    private Vector3 ComputeMoveDirection(Transform playerTransform, Vector3 normalToUse)
    {
        Vector2 _input = m_getRawInput();
        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer(playerTransform, normalToUse, m_cam, _input).normalized;

        if (_dirMoviment.sqrMagnitude > 0.1f)
            m_lastProbeDirection = _dirMoviment;
        else
            _dirMoviment = m_lastProbeDirection;

        return _dirMoviment;
    }

    private void RotateDraco(Transform playerTransform, Vector3 targetNormal)
    {
        float _t = 1f - Mathf.Exp(-m_settings.NormalSlerpSpeed * Time.deltaTime);
        Quaternion _targetRotation = Quaternion.FromToRotation(playerTransform.up, targetNormal) * playerTransform.rotation;
        Quaternion _smoothRotation = Quaternion.Slerp(playerTransform.rotation, _targetRotation, _t);

        if (m_rb != null)
            m_rb.MoveRotation(_smoothRotation);
        else
            playerTransform.rotation = _smoothRotation;
    }
}
