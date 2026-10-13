using UnityEngine;

// Cuida da parte fisica do Draco que vale para qualquer estado:
// ler a superficie, alinhar com a normal e grudar na parede.
public class DracoMotor
{
    private const float k_inputDeadZone = 0.1f;

    private readonly Transform m_transform;
    private readonly Rigidbody m_rigidbody;
    private readonly Transform m_camera;
    private readonly DracoSettings m_settings;

    private readonly WallAndGroundSurfaceProbe m_surfaceProbe;
    private readonly SurfaceNormalTracker m_normalTracker;
    private readonly IMovimentation m_groundMovimentation;
    private float m_detachTimer;

    private Vector3 m_lastSurfaceDirection = Vector3.forward;
    private Vector3 m_groundVelocity;

    public bool IsOnClimbSurface => m_normalTracker.HasNormal;
    public Vector3 SurfaceMoveDirection { get; private set; }
    public Vector3 SnapCorrection { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDetached => m_detachTimer > 0f;

    public DracoMotor(Transform transform, Rigidbody rigidbody, Transform camera, DracoSettings settings)
    {
        m_transform = transform;
        m_rigidbody = rigidbody;
        m_camera = camera;
        m_settings = settings;

        m_groundMovimentation = new Movimentation(m_settings.MoveSpeed, m_settings.AccelerationPlayer, m_settings.DecelerationPlayer);

        m_surfaceProbe = new WallAndGroundSurfaceProbe(m_settings);
        m_normalTracker = new SurfaceNormalTracker(m_settings.NormalGraceTime);
    }

    public void MoveOnSurface(bool hasInput) 
    {
        Vector3 _move = hasInput ? SurfaceMoveDirection * m_settings.MoveSpeed : Vector3.zero;
        m_rigidbody.linearVelocity = _move + SnapCorrection;
    }

    public void MoveOnGround(Vector2 input) 
    {
        Vector3 _direction = CameraRelative.ToWorld(input, m_camera);
        m_groundVelocity = m_groundMovimentation.Move(true, m_groundVelocity, _direction);

        Vector3 _newVelocity = m_rigidbody.linearVelocity;
        _newVelocity.x = m_groundVelocity.x;
        _newVelocity.z = m_groundVelocity.z;
        m_rigidbody.linearVelocity = _newVelocity;
    }

    public void Jump(float force) 
    {
        // Na parede pula para fora e para cima; no chão, só para cima
        Vector3 _dir = Vector3.up;

        if (IsOnClimbSurface)
        {
            _dir = (m_normalTracker.CurrentNormal + Vector3.up).normalized;

            // Solta da parede para ela nao puxar o Draco de volta
            m_normalTracker.Reset();
            Detach(m_settings.WallJumpDetachTime);
        }

        Vector3 _v = m_rigidbody.linearVelocity;
        _v -= Vector3.Project(_v, _dir);          // zera a velocidade nessa direção
        m_rigidbody.linearVelocity = _v + _dir * force;

        IsGrounded = false;
    }
    public void CutJump(float multiplier)         // pulo curto quando solta o botão
    {
        Vector3 _v = m_rigidbody.linearVelocity;
        if (_v.y > 0) _v.y *= multiplier;
        m_rigidbody.linearVelocity = _v;
    }
    public void Detach(float duration) => m_detachTimer = duration;

    // Chamar todo FixedUpdate, antes do estado atual
    public void UpdateSurface(Vector2 input)
    {
        if (m_detachTimer > 0f) 
        {
            m_detachTimer -= Time.fixedDeltaTime;
            m_rigidbody.useGravity = true;
            SnapCorrection = Vector3.zero;
            IsGrounded = false;
            RotateTowards(Vector3.up, Vector3.zero);
            return;
        }

        Vector3 _normalToUse = m_normalTracker.HasNormal ? m_normalTracker.CurrentNormal : m_transform.up;
        SurfaceMoveDirection = ComputeSurfaceDirection(_normalToUse, input);

        SurfaceProbeResult _probeResult = m_surfaceProbe.Probe(m_transform, SurfaceMoveDirection, _normalToUse);
        m_normalTracker.Update(_probeResult, Time.fixedDeltaTime);

        bool _hasInput = input.sqrMagnitude >= k_inputDeadZone;
        RotateTowards(m_normalTracker.CurrentNormal, _hasInput ? SurfaceMoveDirection : Vector3.zero);
        SnapCorrection = ComputeSnapCorrection(_probeResult);
        IsGrounded = ComputeIsGrounded(_probeResult);

        m_rigidbody.useGravity = !m_normalTracker.HasNormal;
    }

    private Vector3 ComputeSurfaceDirection(Vector3 normal, Vector2 input)
    {
        Vector3 _dir = UtilsToAbilityDraco.NewOrientationPlayer(m_transform, normal, m_camera, input).normalized;

        if (_dir.sqrMagnitude > k_inputDeadZone)
            m_lastSurfaceDirection = _dir;
        else
            _dir = m_lastSurfaceDirection;

        return _dir;
    }

    // Unica rotacao do Draco: alinha o "up" com a normal e, se tiver input,
    // vira o "forward" para a direcao do movimento (no plano da superficie).
    // Tem que ser um MoveRotation so por frame, senao um sobrescreve o outro.
    private void RotateTowards(Vector3 targetNormal, Vector3 facingDirection)
    {
        float _t = 1f - Mathf.Exp(-m_settings.NormalSlerpSpeed * Time.fixedDeltaTime);

        Quaternion _currentRotation = m_rigidbody.rotation;
        Vector3 _currentUp = _currentRotation * Vector3.up;

        // Sem input: menor rotacao que leva o up atual para a normal (mantem para onde olha)
        Quaternion _targetRotation = Quaternion.FromToRotation(_currentUp, targetNormal) * _currentRotation;

        // Com input: olha para onde anda, usando a normal como up
        Vector3 _forward = Vector3.ProjectOnPlane(facingDirection, targetNormal);
        if (_forward.sqrMagnitude > 0.001f)
            _targetRotation = Quaternion.LookRotation(_forward.normalized, targetNormal);

        m_rigidbody.MoveRotation(Quaternion.Slerp(_currentRotation, _targetRotation, _t));
    }

    // O probe alcanca longe (GroundProbeDistance), entao "achou chao" nao significa
    // "esta no chao": so conta se o pe estiver perto dele.
    private bool ComputeIsGrounded(SurfaceProbeResult data)
    {
        if (!data.GroundHitFound) return false;

        float _distanceToGround = Vector3.Dot(m_transform.position - data.GroundHit.point, data.GroundHit.normal);
        return _distanceToGround <= m_settings.GroundedDistance;
    }

    private Vector3 ComputeSnapCorrection(SurfaceProbeResult data)
    {
        if (!data.GroundHitFound) return Vector3.zero;

        float _distanceToGround = Vector3.Dot(m_transform.position - data.GroundHit.point, data.GroundHit.normal);

        if (_distanceToGround > m_settings.SnapDistanceMin)
            return -data.GroundHit.normal * m_settings.SnapForce;

        return Vector3.zero;
    }
}
