using System;
using System.Collections.Generic;
using UnityEngine;

public class DracoWalkInWallsAbility : IAbilityPlayer {
    private Func<Vector2> m_getRawInput;
    private Transform m_cam;
    CurveRaycast m_curveRay;
    NormalsDracoNeedToRotate m_normalsDraco;
    Rigidbody m_rb;

    private Vector3 m_lastKnownNormal;
    private bool m_hasNormal = false;

    private Vector3 m_lastForwardReference;
    private bool m_hasForwardReference = false;
    private const float k_forwardSlerpSpeed = 15f;

    private const float k_normalSlerpSpeed = 15;
    private const float k_distanceWeightExponent = 2f;

    public DracoWalkInWallsAbility(Func<Vector2> rawInput, Transform cam, Rigidbody rb) {
        if (m_curveRay == null) {
            m_curveRay = new CurveRaycast();
        }
        if (m_normalsDraco == null) {
            m_normalsDraco = new NormalsDracoNeedToRotate();
        }
        m_getRawInput = rawInput;
        m_cam = cam;
        m_rb = rb;
    }

    public void ExecuteAbilityUpdate(Transform playerTransform) {
        int _countRays = 13;
        Vector2 _input = m_getRawInput();

        Vector3 _normalToUse = m_hasNormal ? m_lastKnownNormal : playerTransform.up;
        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer
        (
            playerTransform,
            m_cam,
            _normalToUse,
            _input,
            ref m_lastForwardReference,
            ref m_hasForwardReference,
            k_forwardSlerpSpeed
        ).normalized;

        DataCurveRaycast _dataCurveRay = m_curveRay.GetCurveRaycast(playerTransform, _dirMoviment, _normalToUse, _countRays);
        DataNormalsDracoNeedToRotate _dataNormalsDraco = m_normalsDraco.GetNormalsDracoNeedToRotate(playerTransform, _dirMoviment, _normalToUse);

        List<SurfaceSample> _samples = new List<SurfaceSample>();

        if (_dataNormalsDraco.GroundHitFound) {
            float _dist = Vector3.Distance(playerTransform.position, _dataNormalsDraco.GroundHit.point);
            _samples.Add(new SurfaceSample(_dataNormalsDraco.GroundHit.normal, _dist, "ground"));
        }

        if (_dataNormalsDraco.WallHitFound) {
            float _dist = Vector3.Distance(playerTransform.position, _dataNormalsDraco.WallHit.point);
            _samples.Add(new SurfaceSample(_dataNormalsDraco.WallHit.normal, _dist, "wall"));
        }

        if (_dataCurveRay.m_curveRaycastFound) {
            float _dist = Vector3.Distance(playerTransform.position, _dataCurveRay.m_curveRaycastInDirectionToMove.point);
            _samples.Add(new SurfaceSample(_dataCurveRay.m_curveRaycastInDirectionToMove.normal, _dist, "curve"));
        }

        m_rb.linearVelocity = _dirMoviment * 5;

        Vector3 _weightedNormal = CalculateWeightedNormal(_samples, _normalToUse);

        if (!m_hasNormal) {
            m_lastKnownNormal = _weightedNormal;
        }
        else {
            m_lastKnownNormal = Vector3.Slerp(m_lastKnownNormal, _weightedNormal, k_normalSlerpSpeed * Time.deltaTime);
        }
        m_hasNormal = _samples.Count > 0 || m_hasNormal;

        Debug.DrawRay(playerTransform.position, m_lastKnownNormal * 1.5f, Color.azure);
    }

    private Vector3 CalculateWeightedNormal(List<SurfaceSample> samples, Vector3 fallbackNormal) {
        if (samples.Count == 0) {
            return fallbackNormal;
        }

        const float k_minDistance = 0.001f;
        Vector3 _weightedSum = Vector3.zero;
        float _totalWeight = 0f;

        foreach (SurfaceSample _sample in samples) {
            float _safeDistance = Mathf.Max(_sample.Distance, k_minDistance);
            float _weight = 1f / Mathf.Pow(_safeDistance, k_distanceWeightExponent);

            _weightedSum += _sample.Normal * _weight;
            _totalWeight += _weight;
        }

        Vector3 _average = _weightedSum / _totalWeight;
        return _average.normalized;
    }
}
