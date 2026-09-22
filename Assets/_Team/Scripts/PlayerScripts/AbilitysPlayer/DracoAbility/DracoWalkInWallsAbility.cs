using System;
using UnityEngine;

public class DracoWalkInWallsAbility : IAbilityPlayer 
{
    private Func<Vector2> m_getRawInput;
    private Transform m_cam;
    private NormalsDracoNeedToRotate m_normalsDraco;
    private Rigidbody m_rb;
    private LayerMask m_layerMask;

    private Vector3 m_lastKnownNormal;
    private bool m_hasNormal = false;

    private Vector3 m_lastProbeDirection = Vector3.forward;
    private const float k_normalSlerpSpeed = 12f;

    private float m_timeSinceLastHit = 0f;
    private const float k_normalGraceTime = 0.35f;

    public DracoWalkInWallsAbility(Func<Vector2> rawInput, Transform cam, Rigidbody rb) 
    {
        m_normalsDraco = new NormalsDracoNeedToRotate();
        m_getRawInput = rawInput;
        m_cam = cam;
        m_rb = rb;
    }

    public void ExecuteAbilityUpdate(Transform playerTransform) 
    {
        MoveThroughWall(playerTransform);      
    }

    void MoveThroughWall(Transform playerTransform) 
    {
        Vector2 _input = m_getRawInput();
        Vector3 _normalToUse = m_hasNormal ? m_lastKnownNormal : playerTransform.up;
        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer(playerTransform, _normalToUse, m_cam, _input).normalized;
        Vector3 _probeDirection = _dirMoviment.sqrMagnitude > 0.1f ? _dirMoviment : m_lastProbeDirection;
        m_lastProbeDirection = _probeDirection;
        DataNormalsDracoNeedToRotate _dataNormalsDraco = m_normalsDraco.GetNormalsDracoNeedToRotate(playerTransform, _probeDirection, _normalToUse);

        float t = 1f - Mathf.Exp(-k_normalSlerpSpeed * Time.deltaTime);

        if (_dataNormalsDraco.WallHitFound) 
        {
            m_lastKnownNormal = _dataNormalsDraco.WallHit.normal;
            m_hasNormal = true;
            m_timeSinceLastHit = 0f;
        }
        else if (_dataNormalsDraco.GroundHitFound) 
        {
            m_lastKnownNormal = _dataNormalsDraco.GroundHit.normal;
            m_hasNormal = true;
            m_timeSinceLastHit = 0f;
        }
        else 
        {
            m_timeSinceLastHit += Time.deltaTime;
            if (m_timeSinceLastHit >= k_normalGraceTime) 
            {
                m_lastKnownNormal = Vector3.up;
                m_hasNormal = false;
            }
        }

        RotateDraco(playerTransform, m_lastKnownNormal, t);

        Vector3 _snapCorrection = ComputeSnapCorrection(playerTransform, _dataNormalsDraco);

        m_rb.linearVelocity = _dirMoviment * 4 + _snapCorrection;
    }

    void RotateDraco(Transform playerTransform, Vector3 targetNormal, float t) 
    {
        Quaternion _targetRotation = Quaternion.FromToRotation(playerTransform.up, targetNormal) * playerTransform.rotation;
        Quaternion _smoothRotation = Quaternion.Slerp(playerTransform.rotation, _targetRotation, t);

        if (m_rb != null)
            m_rb.MoveRotation(_smoothRotation);
        else
            playerTransform.rotation = _smoothRotation;
    }

    Vector3 ComputeSnapCorrection(Transform playerTransform, DataNormalsDracoNeedToRotate data) 
    {
        if (!data.GroundHitFound) return Vector3.zero;

        float _distanceToGround = Vector3.Dot(
            playerTransform.position - data.GroundHit.point,
            data.GroundHit.normal
        );

        float _distanceMin = 0.085f;
        float _force = 6;

        if (_distanceToGround > _distanceMin)
            return -data.GroundHit.normal * _force;

        return Vector3.zero;
    }
}
