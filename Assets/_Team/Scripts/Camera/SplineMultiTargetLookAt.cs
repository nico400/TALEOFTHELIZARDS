using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Controls the LookAt focus of a Spline Dolly camera by smoothly blending between
/// multiple points of interest based on the camera's position along the spline.
/// 
/// Single Responsibility: Multi-target interpolation along a spline.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("Camera/Spline Multi-Target LookAt")]
public class SplineMultiTargetLookAt : MonoBehaviour
{
    [System.Serializable]
    public struct TargetPoint
    {
        [Tooltip("Object to look at.")]
        public Transform target;

        [Tooltip("Normalized position along the spline (0 to 1) where this target has maximum influence.")]
        [Range(0f, 1f)]
        public float splinePosition;

        [Tooltip("Local offset applied to target position (e.g., aim at eye level).")]
        public Vector3 offset;
    }

    [Header("References")]
    [Tooltip("Cinemachine Camera traversing the spline.")]
    [SerializeField] private CinemachineCamera m_cinemachineCamera;

    [Tooltip("CinemachineSplineDolly component (if on another GameObject or not detected automatically).")]
    [SerializeField] private CinemachineSplineDolly m_splineDolly;

    [Tooltip("SplineAnimate component (if using Unity Splines animate component instead of CinemachineSplineDolly).")]
    [SerializeField] private SplineAnimate m_splineAnimate;

    [Header("Targets Along Spline")]
    [Tooltip("List of targets ordered by spline position (0.0 to 1.0).")]
    [SerializeField] private List<TargetPoint> m_targets = new();

    [Header("Smoothing")]
    [Tooltip("How fast the camera shifts its gaze between targets (lower = faster, higher = smoother).")]
    [SerializeField, Min(0.01f)] private float m_smoothTime = 0.4f;

    [Tooltip("Treat the spline as a continuous closed loop (blends seamlessly from 1.0 back to 0.0).")]
    [SerializeField] private bool m_isLoop = true;

    [Header("Debug / Gizmos")]
    [SerializeField] private bool m_showGizmos = true;

    private Transform m_lookAtProxy;
    private Vector3 m_currentFocusPosition;
    private Vector3 m_velocity;

    private void Awake()
    {
        EnsureReferences();
        EnsureProxyTarget();
    }

    private void OnValidate()
    {
        EnsureReferences();
    }

    private void Update()
    {
        if (m_targets == null || m_targets.Count == 0) return;

        EnsureReferences();
        EnsureProxyTarget();

        float progress = GetCurrentSplineProgress();
        Vector3 desiredPosition = CalculateTargetPosition(progress);

        if (Application.isPlaying)
        {
            m_currentFocusPosition = Vector3.SmoothDamp(
                m_currentFocusPosition,
                desiredPosition,
                ref m_velocity,
                m_smoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }
        else
        {
            m_currentFocusPosition = desiredPosition;
        }

        if (m_lookAtProxy != null)
        {
            m_lookAtProxy.position = m_currentFocusPosition;
        }
    }

    /// <summary>
    /// Gets current progress along the spline as a normalized value (0.0 to 1.0).
    /// </summary>
    private float GetCurrentSplineProgress()
    {
        if (m_splineDolly != null)
        {
            float pos = m_splineDolly.CameraPosition;

            // Handle different position unit modes in Cinemachine 3
            if (m_splineDolly.PositionUnits == PathIndexUnit.Normalized)
            {
                return Mathf.Repeat(pos, 1f);
            }

            if (m_splineDolly.Spline != null && m_splineDolly.Spline.Spline != null)
            {
                float totalLength = m_splineDolly.Spline.Spline.GetLength();
                if (totalLength > 0.001f)
                    return Mathf.Repeat(pos / totalLength, 1f);
            }

            return Mathf.Repeat(pos, 1f);
        }

        if (m_splineAnimate != null)
        {
            return Mathf.Repeat(m_splineAnimate.NormalizedTime, 1f);
        }

        return 0f;
    }

    /// <summary>
    /// Blends positions between the two surrounding waypoints based on current spline progress.
    /// </summary>
    private Vector3 CalculateTargetPosition(float progress)
    {
        if (m_targets.Count == 1)
        {
            return GetWorldPoint(m_targets[0]);
        }

        // Sort targets by spline position in ascending order
        List<TargetPoint> sorted = new(m_targets);
        sorted.Sort((a, b) => a.splinePosition.CompareTo(b.splinePosition));

        TargetPoint prev = sorted[^1];
        TargetPoint next = sorted[0];

        // Find surrounding points
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].splinePosition <= progress)
            {
                prev = sorted[i];
                next = (i + 1 < sorted.Count) ? sorted[i + 1] : sorted[0];
            }
        }

        float span = next.splinePosition - prev.splinePosition;
        if (span <= 0f && m_isLoop)
        {
            span += 1f; // Wrap around for closed loops
        }

        float delta = progress - prev.splinePosition;
        if (delta < 0f && m_isLoop)
        {
            delta += 1f;
        }

        float t = (span > 0.0001f) ? Mathf.Clamp01(delta / span) : 0f;

        // Smooth step for natural camera acceleration and deceleration
        t = Mathf.SmoothStep(0f, 1f, t);

        Vector3 posA = GetWorldPoint(prev);
        Vector3 posB = GetWorldPoint(next);

        return Vector3.Lerp(posA, posB, t);
    }

    private Vector3 GetWorldPoint(TargetPoint pt)
    {
        if (pt.target != null)
            return pt.target.TransformPoint(pt.offset);
        return pt.offset;
    }

    private void EnsureReferences()
    {
        if (m_cinemachineCamera == null)
            TryGetComponent(out m_cinemachineCamera);

        if (m_splineDolly == null)
            TryGetComponent(out m_splineDolly);

        if (m_splineAnimate == null)
            TryGetComponent(out m_splineAnimate);
    }

    private void EnsureProxyTarget()
    {
        if (m_lookAtProxy == null)
        {
            string proxyName = $"{gameObject.name}_LookAtProxy";
            GameObject existing = GameObject.Find(proxyName);
            if (existing != null)
            {
                m_lookAtProxy = existing.transform;
            }
            else
            {
                GameObject go = new(proxyName);
                m_lookAtProxy = go.transform;
                m_lookAtProxy.hideFlags = HideFlags.DontSave;
            }
        }

        // Assign proxy as the LookAt target on CinemachineCamera
        if (m_cinemachineCamera != null && m_cinemachineCamera.Target.LookAtTarget != m_lookAtProxy)
        {
            m_cinemachineCamera.Target.LookAtTarget = m_lookAtProxy;
        }
    }

    private void OnDrawGizmos()
    {
        if (!m_showGizmos || m_targets == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < m_targets.Count; i++)
        {
            Vector3 pt = GetWorldPoint(m_targets[i]);
            Gizmos.DrawWireSphere(pt, 0.4f);

            if (i + 1 < m_targets.Count)
            {
                Gizmos.DrawLine(pt, GetWorldPoint(m_targets[i + 1]));
            }
            else if (m_isLoop && m_targets.Count > 1)
            {
                Gizmos.DrawLine(pt, GetWorldPoint(m_targets[0]));
            }
        }

        // Current focus point
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(m_currentFocusPosition, 0.25f);
        Gizmos.DrawLine(transform.position, m_currentFocusPosition);
    }
}

