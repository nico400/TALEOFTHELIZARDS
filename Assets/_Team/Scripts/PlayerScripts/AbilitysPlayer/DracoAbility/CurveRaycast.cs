using System.Collections.Generic;
using UnityEngine;

public class CurveRaycast {
    float m_distanceRay = 1.25f;
    Vector3 m_lastDirMoviment;
    LayerMask m_maskToNotDetectedHit = ~LayerMask.GetMask("Players");

    private const float k_minProjectionSqrMagnitude = 0.001f;

    public DataCurveRaycast GetCurveRaycast(Transform playerTransform, Vector3 dirMoviment, Vector3 normalUsedForDirection, int totalRays) 
    {
        DataCurveRaycast _data = new DataCurveRaycast();
        float _valueToIncrementAngle = 360f / totalRays;
        float _startAngle = 90;

        if (dirMoviment.magnitude > 0.01f) 
        {
            m_lastDirMoviment = -dirMoviment;
        }

        // m_lastDirMoviment pode estar "desatualizado" (ex: última direção
        // válida foi calculada numa superfície diferente da atual). Projetamos
        // no plano perpendicular ao normal atual para garantir ortogonalidade
        // antes de usar como forward no LookRotation — evita a singularidade
        // de forward/up paralelos.
        Vector3 _safeForward = Vector3.ProjectOnPlane(m_lastDirMoviment, normalUsedForDirection);

        if (_safeForward.sqrMagnitude < k_minProjectionSqrMagnitude) 
        {
            // Fallback: m_lastDirMoviment está paralelo demais ao normal atual.
            // Tentamos o eixo mundial "forward" como referência alternativa.
            _safeForward = Vector3.ProjectOnPlane(Vector3.forward, normalUsedForDirection);

            if (_safeForward.sqrMagnitude < k_minProjectionSqrMagnitude) 
            {
                // Último recurso: Vector3.forward também degenerou (raro,
                // só se o normal apontar quase exatamente no eixo Z do mundo).
                _safeForward = Vector3.ProjectOnPlane(Vector3.right, normalUsedForDirection);
            }
        }

        _safeForward.Normalize();

        Quaternion _rotationToDirection = Quaternion.LookRotation(_safeForward, normalUsedForDirection);

        Vector3 _previusPoint = Vector3.zero;
        for (int i = 0; i <= totalRays; i++) 
        {
            int _index = i % totalRays;
            float _currentAngleDeg = _startAngle + (_valueToIncrementAngle * _index);
            float _currentAngle = _currentAngleDeg * Mathf.Deg2Rad;
            float _currentSin = Mathf.Sin(_currentAngle);
            float _currentCos = Mathf.Cos(_currentAngle);
            Vector3 _currentVectorOfAngle = new Vector3(0, _currentSin, _currentCos);
            Vector3 _currentRotateVectorRays = _rotationToDirection * _currentVectorOfAngle;
            Vector3 _currentPoint = playerTransform.position + (_currentRotateVectorRays * m_distanceRay);

            if (i > 0) 
            {
                Vector3 _dirLastPointToCurrent = _currentPoint - _previusPoint;
                _data.m_curveRaycastFound = Physics.Raycast
                    (
                        _previusPoint,
                        _dirLastPointToCurrent,
                        out _data.m_curveRaycastInDirectionToMove,
                        _dirLastPointToCurrent.magnitude,
                        m_maskToNotDetectedHit
                    );
                Debug.DrawRay(_previusPoint, _dirLastPointToCurrent, Color.blue);
            }

            if (_data.m_curveRaycastFound)
                break;

            _previusPoint = _currentPoint;
        }
        return _data;
    }
}
