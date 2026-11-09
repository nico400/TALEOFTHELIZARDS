using UnityEngine;

public static class UtilsToAbilityDraco
{
    // Converte o input em direcao sobre a superficie, relativa a camera.
    // Os eixos sao misturados de forma continua (sem normalizar vetores quase nulos
    // e sem troca brusca de referencia), senao a direcao flica quando a camera
    // olha ao longo da parede (cam.right quase paralelo a normal).
    public static Vector3 NewOrientationPlayer(Transform playerTransform, Vector3 normal, Transform cam, Vector2 input)
    {
        // Frente: cam.forward projetado; quanto mais a camera encara a superficie,
        // mais o cam.up assume (olhando de frente pra parede, frente = subir)
        float _facing = Mathf.Abs(Vector3.Dot(cam.forward, normal));
        Vector3 _forwardPlayer = Vector3.ProjectOnPlane(cam.forward, normal)
                               + Vector3.ProjectOnPlane(cam.up, normal) * _facing;

        // Direita: cam.right projetado; quando cam.right aponta pra dentro/fora da
        // superficie, o perpendicular a frente assume (empurrar contra a parede = subir)
        float _side = Mathf.Abs(Vector3.Dot(cam.right, normal));
        Vector3 _rightPlayer = Vector3.ProjectOnPlane(cam.right, normal)
                             + Vector3.Cross(normal, _forwardPlayer.normalized) * _side;

        _forwardPlayer.Normalize();
        _rightPlayer.Normalize();

        Vector2 inputClamped = Vector2.ClampMagnitude(input, 1f); // evita diagonal mais rápida

        Vector3 dir = _forwardPlayer * inputClamped.y + _rightPlayer * inputClamped.x;

        Debug.DrawRay(playerTransform.position, _rightPlayer * 2f, Color.red);
        Debug.DrawRay(playerTransform.position, _forwardPlayer * 2f, Color.green);

        return dir;
    }
}
