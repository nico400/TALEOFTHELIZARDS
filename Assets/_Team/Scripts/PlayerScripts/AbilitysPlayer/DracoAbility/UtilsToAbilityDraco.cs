using UnityEngine;

public static class UtilsToAbilityDraco {
    private const float k_minProjectionSqrMagnitude = 0.015f;

    public static Vector3 NewOrientationPlayer(
        Transform playerTransform,
        Transform cam,
        Vector3 normal,
        Vector2 input,
        ref Vector3 lastForwardReference,
        ref bool hasForwardReference,
        float forwardSlerpSpeed) {
        Vector3 _blendedForward = GetBlendedForwardOnPlane(cam, normal);

        Vector3 _forward;
        if (!hasForwardReference) {
            _forward = _blendedForward;
        }
        else {
            _forward = Vector3.Slerp(lastForwardReference, _blendedForward, forwardSlerpSpeed * Time.deltaTime);
        }

        lastForwardReference = _forward;
        hasForwardReference = true;

        Vector3 _right = Vector3.ProjectOnPlane(cam.right, normal).normalized;

        Debug.DrawRay(playerTransform.position, _right * 2, Color.green);
        Debug.DrawRay(playerTransform.position, _forward * 2, Color.yellow);

        // A projeção em um plano é a mesma para "normal" e "-normal" (mesmo plano),
        // então sem essa correção W anda pro mesmo lado no chão e no teto.
        // Aqui a gente detecta se a superfície está "de cabeça pra baixo" em relação
        // ao mundo e inverte o eixo de frente/trás (input.y) nesse caso.
        float _verticalOrientationSign = Mathf.Sign(Vector3.Dot(normal, Vector3.up));

        Vector3 _dirMoviment = (_forward * (input.y * _verticalOrientationSign) + _right * input.x).normalized;
        Debug.DrawRay(playerTransform.position, _dirMoviment * 2, Color.red);

        return _dirMoviment;
    }

    private static Vector3 GetBlendedForwardOnPlane(Transform cam, Vector3 normal) {
        Vector3 _fromWorldUp = Vector3.ProjectOnPlane(Vector3.up, normal);

        Vector3 _fromCamForward = Vector3.ProjectOnPlane(cam.forward, normal);

        Vector3 _blended = _fromWorldUp + _fromCamForward;

        if (_blended.sqrMagnitude >= k_minProjectionSqrMagnitude) {
            return _blended.normalized;
        }
        return Vector3.ProjectOnPlane(cam.right, normal).normalized;
    }
}
