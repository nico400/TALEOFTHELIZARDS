using UnityEngine;

// Cuida da parte fisica do Quirino que vale para qualquer estado:
// checar o chao, andar e pular. Ele nao escala parede.
public class QuirinoMotor
{
    // A esfera comeca um pouco acima do pe: o SphereCast ignora o que ja esta
    // encostando nela no inicio, entao comecar colado no chao nao detectaria nada
    private const float k_castOffset = 0.1f;

    private readonly Transform m_transform;
    private readonly Rigidbody m_rigidbody;
    private readonly Transform m_camera;
    private readonly QuirinoSettings m_settings;
    private readonly IMovimentation m_groundMovimentation;
    private readonly LayerMask m_groundLayerMask;

    private Vector3 m_groundVelocity;

    public bool IsGrounded { get; private set; }

    public QuirinoMotor(Transform transform, Rigidbody rigidbody, Transform camera, QuirinoSettings settings)
    {
        m_transform = transform;
        m_rigidbody = rigidbody;
        m_camera = camera;
        m_settings = settings;

        m_groundMovimentation = new Movimentation(m_settings.MoveSpeed, m_settings.AccelerationPlayer, m_settings.DecelerationPlayer);
        m_groundLayerMask = ~LayerMask.GetMask("Players");
    }

    // Anda e olha para onde esta andando
    public void MoveOnGround(Vector2 input)
    {
        Vector3 _direction = CameraRelative.ToWorld(input, m_camera);
        ApplyGroundVelocity(_direction);
        RotateTowards(_direction);
    }

    // Anda, mas olha para facingDirection (ex.: mirando, olha para frente da camera)
    public void MoveOnGround(Vector2 input, Vector3 facingDirection)
    {
        ApplyGroundVelocity(CameraRelative.ToWorld(input, m_camera));
        RotateTowards(facingDirection);
    }

    // Desacelera ate parar sem rotacionar (ex.: enquanto usa a lingua)
    public void StopOnGround()
    {
        ApplyGroundVelocity(Vector3.zero);
    }

    private void ApplyGroundVelocity(Vector3 direction)
    {
        m_groundVelocity = m_groundMovimentation.Move(true, m_groundVelocity, direction);

        Vector3 _newVelocity = m_rigidbody.linearVelocity;
        _newVelocity.x = m_groundVelocity.x;
        _newVelocity.z = m_groundVelocity.z;
        m_rigidbody.linearVelocity = _newVelocity;
    }

    // Vira o Quirino para onde ele esta andando. Ele nao escala,
    // entao o up e sempre o Vector3.up e so gira no plano do chao.
    private void RotateTowards(Vector3 facingDirection)
    {
        Vector3 _forward = Vector3.ProjectOnPlane(facingDirection, Vector3.up);
        if (_forward.sqrMagnitude < 0.001f) return; // sem input: mantem para onde olha

        float _t = 1f - Mathf.Exp(-m_settings.RotationSpeed * Time.fixedDeltaTime);

        Quaternion _currentRotation = m_rigidbody.rotation;
        Quaternion _targetRotation = Quaternion.LookRotation(_forward.normalized, Vector3.up);
        m_rigidbody.MoveRotation(Quaternion.Slerp(_currentRotation, _targetRotation, _t));
    }

    // Chamar todo FixedUpdate, antes do estado atual
    public void UpdateGround()
    {
        float _radius = m_settings.GroundCheckRadius;
        Vector3 _origin = m_transform.position + Vector3.up * (_radius + k_castOffset);
        float _distance = k_castOffset + m_settings.GroundCheckDistance;

        IsGrounded = Physics.SphereCast(_origin, _radius, Vector3.down, out _, _distance, m_groundLayerMask, QueryTriggerInteraction.Ignore);
    }

    public void Jump(float force)
    {
        Vector3 _v = m_rigidbody.linearVelocity;
        _v.y = 0f;                                // zera a velocidade vertical antes do impulso
        m_rigidbody.linearVelocity = _v + Vector3.up * force;

        IsGrounded = false;
    }

    public void CutJump(float multiplier)         // pulo curto quando solta o botão
    {
        Vector3 _v = m_rigidbody.linearVelocity;
        if (_v.y > 0) _v.y *= multiplier;
        m_rigidbody.linearVelocity = _v;
    }
}
