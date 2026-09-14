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
        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer(playerTransform, m_cam, _normalToUse, _input, ref m_lastForwardReference, ref m_hasForwardReference, k_forwardSlerpSpeed).normalized;

        Vector3 _probeDirection = _dirMoviment.sqrMagnitude > 0.0001f ? _dirMoviment : m_lastProbeDirection;
        m_lastProbeDirection = _probeDirection;

        DataNormalsDracoNeedToRotate _dataNormalsDraco = m_normalsDraco.GetNormalsDracoNeedToRotate(playerTransform, _probeDirection, _normalToUse);

        if (_dataNormalsDraco.WallHitFound) 
        {
            m_lastKnownNormal = m_hasNormal
                ? Vector3.Slerp(m_lastKnownNormal, _dataNormalsDraco.WallHit.normal, k_normalSlerpSpeed * Time.deltaTime)
                : _dataNormalsDraco.WallHit.normal;
            m_hasNormal = true;
        }
        else if (_dataNormalsDraco.GroundHitFound) 
        {
            m_lastKnownNormal = m_hasNormal
                ? Vector3.Slerp(m_lastKnownNormal, _dataNormalsDraco.GroundHit.normal, k_normalSlerpSpeed * Time.deltaTime)
                : _dataNormalsDraco.GroundHit.normal;
            m_hasNormal = true;
        }

        if (!_dataNormalsDraco.GroundHitFound && !_dataNormalsDraco.WallHitFound)
            m_hasNormal = false;


        m_rb.linearVelocity = _dirMoviment * 5;

        Debug.DrawRay(playerTransform.position, _dirMoviment * 2f, Color.red);
        Debug.DrawRay(playerTransform.position, _normalToUse * 2f, Color.green);
    }
}
