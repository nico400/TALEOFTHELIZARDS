using System;
using Unity.Cinemachine;
using UnityEngine;

public class DracoWalkInWallsAbility : IAbilityPlayer 
{
    private Func<Vector2> m_getRawInput;
    private Transform m_cam;
    CurveRaycast m_curveRay;
    NormalsDracoNeedToRotate m_normalsDraco;
    Rigidbody m_rb;

    private Vector3 m_lastKnownNormal;
    private bool m_hasNormal = false;

    public DracoWalkInWallsAbility(Func<Vector2> rawInput, Transform cam, Rigidbody rb) 
    {
        if (m_curveRay == null) 
        {
            m_curveRay = new CurveRaycast();
        }
        if (m_normalsDraco == null) 
        {
            m_normalsDraco = new NormalsDracoNeedToRotate();
        }
        m_getRawInput = rawInput;
        m_cam = cam;
        m_rb = rb;
    }

    public void ExecuteAbilityUpdate(Transform playerTransform) 
    {
        int _countRays = 13;
        Vector2 _input = m_getRawInput();

        Vector3 _normalToUse = m_hasNormal ? m_lastKnownNormal : playerTransform.up;
        Vector3 _dirMoviment = UtilsToAbilityDraco.NewOrientationPlayer(playerTransform, m_cam, _normalToUse, _input).normalized;

        DataCurveRaycast _dataCurveRay = m_curveRay.GetCurveRaycast(playerTransform, _dirMoviment, _normalToUse, _countRays);
        DataNormalsDracoNeedToRotate _dataNormalsDraco = m_normalsDraco.GetNormalsDracoNeedToRotate(playerTransform, _dirMoviment, _normalToUse);

    }
}
