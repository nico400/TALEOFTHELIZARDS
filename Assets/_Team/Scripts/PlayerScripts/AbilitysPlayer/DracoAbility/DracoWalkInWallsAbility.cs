using System;
using System.Collections.Generic;
using UnityEngine;

public class DracoWalkInWallsAbility : IAbilityPlayer {
    private Func<Vector2> m_getRawInput;
    private Transform m_cam;
    private CurveRaycast m_curveRay;
    private NormalsDracoNeedToRotate m_normalsDraco;
    private Rigidbody m_rb;
    private List<SurfaceSample> m_samples = new List<SurfaceSample>();

    private Vector3 m_lastKnownNormal;
    private bool m_hasNormal = false;

    private float m_normalLostTimer = 0f;
    private const float k_normalLostGrace = 0.15f;

    private Vector3 m_lastForwardReference;
    private bool m_hasForwardReference = false;
    private const float k_forwardSlerpSpeed = 15f;

    private float m_lastVerticalSign = 1f;

    private Vector3 m_lastProbeDirection = Vector3.forward;
    private const float k_normalSlerpSpeed = 12f;

    public DracoWalkInWallsAbility(Func<Vector2> rawInput, Transform cam, Rigidbody rb) {
        m_curveRay = new CurveRaycast();
        m_normalsDraco = new NormalsDracoNeedToRotate();
        m_getRawInput = rawInput;
        m_cam = cam;
        m_rb = rb;
    }

    public void ExecuteAbilityUpdate(Transform playerTransform) 
    {
        Vector2 _input = m_getRawInput();

        Vector3 _normalToUse = m_hasNormal ? m_lastKnownNormal : playerTransform.up;

        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer(playerTransform, _normalToUse, m_cam, _input).normalized;

        // fallback simples: reusa a última direção válida quando o input for zero
        Vector3 _probeDirection = _dirMoviment.sqrMagnitude > 0.1f ? _dirMoviment : m_lastProbeDirection;
        m_lastProbeDirection = _probeDirection;

        Debug.DrawRay(playerTransform.position, _probeDirection * 5f, Color.red);

        DataNormalsDracoNeedToRotate _dataNormalsDraco = m_normalsDraco.GetNormalsDracoNeedToRotate(playerTransform, _probeDirection, _normalToUse);

        float t = 1f - Mathf.Exp(-k_normalSlerpSpeed * Time.deltaTime);

        if (_dataNormalsDraco.WallHitFound) 
        {
            m_normalLostTimer = 0f;
            m_lastKnownNormal = m_hasNormal
                ? Vector3.Slerp(m_lastKnownNormal, _dataNormalsDraco.WallHit.normal, t)
                : _dataNormalsDraco.WallHit.normal;
            m_hasNormal = true;
        }
        else if (_dataNormalsDraco.GroundHitFound) 
        {
            m_normalLostTimer = 0f;
            m_lastKnownNormal = m_hasNormal
                ? Vector3.Slerp(m_lastKnownNormal, _dataNormalsDraco.GroundHit.normal, t)
                : _dataNormalsDraco.GroundHit.normal;
            m_hasNormal = true;
        }
        else 
        {
            m_normalLostTimer += Time.deltaTime;
            if (m_normalLostTimer > k_normalLostGrace)
                m_hasNormal = false;
        }

        Debug.Log($"HasNormal: {m_hasNormal} | Normal: {m_lastKnownNormal}");

        m_rb.linearVelocity = _dirMoviment * 5;
    }
}
