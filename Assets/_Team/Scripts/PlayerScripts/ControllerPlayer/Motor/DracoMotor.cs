using UnityEngine;

// Cuida da parte fisica do Draco que vale para qualquer estado:
// ler a superficie, alinhar com a normal e grudar na parede.
//
// A escalada segue o Unity-Procedural-IK-Wall-Walking-Spider (PhilS94):
//  - os casts saem do centro do collider na direcao do CORPO (forward e -up)
//  - o Draco gruda com uma "gravidade falsa" ao longo da normal, que desliga
//    quando ja esta encostado (GravityOffDistance). Nao existe snap empurrando
//    contra a parede o tempo todo.
//  - ao longo da superficie so o input move: qualquer deslize criado pela
//    fisica (colisao, depenetracao) e descartado todo frame.
public class DracoMotor
{
    private const float k_inputDeadZone = 0.1f;

    private readonly Transform m_transform;
    private readonly Rigidbody m_rigidbody;
    private readonly CapsuleCollider m_collider;
    private readonly Transform m_camera;
    private readonly DracoSettings m_settings;

    private readonly WallAndGroundSurfaceProbe m_surfaceProbe;
    private readonly SurfaceNormalTracker m_normalTracker;
    private readonly IMovimentation m_groundMovimentation;
    private float m_detachTimer;

    private Vector3 m_groundVelocity;
    private float m_surfaceFallSpeed; // velocidade da gravidade falsa contra a superficie

    public bool IsOnClimbSurface => m_normalTracker.HasNormal;
    public Vector3 SurfaceMoveDirection { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDetached => m_detachTimer > 0f;

    public DracoMotor(Transform transform, Rigidbody rigidbody, CapsuleCollider collider, Transform camera, DracoSettings settings)
    {
        m_transform = transform;
        m_rigidbody = rigidbody;
        m_collider = collider;
        m_camera = camera;
        m_settings = settings;

        m_groundMovimentation = new Movimentation(m_settings.MoveSpeed, m_settings.AccelerationPlayer, m_settings.DecelerationPlayer);

        m_surfaceProbe = new WallAndGroundSurfaceProbe(m_settings, collider);
        m_normalTracker = new SurfaceNormalTracker(m_settings.NormalGraceTime);
    }

    public void MoveOnSurface(bool hasInput)
    {
        Vector3 _move = hasInput ? SurfaceMoveDirection * m_settings.MoveSpeed : Vector3.zero;
        m_rigidbody.linearVelocity = _move - m_normalTracker.CurrentNormal * m_surfaceFallSpeed;
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

        m_surfaceFallSpeed = 0f;

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

    public void DrawProbeGizmos() => m_surfaceProbe.DrawGizmos(m_transform);

    // Esquece parede e chao ao sair do motor de superficie (ex: subir na lingua),
    // senao ao voltar ele acha que ainda esta grudado
    public void ClearSurface()
    {
        m_normalTracker.Reset();
        m_detachTimer = 0f;
        m_groundVelocity = Vector3.zero;
        m_surfaceFallSpeed = 0f;
        IsGrounded = false;
    }

    // Leva o Draco ate um ponto pela velocidade (respeita colisao) e alinha com up/facing.
    // Usado por estados que nao usam o UpdateSurface, como andar na lingua.
    public void FollowPoint(Vector3 targetPosition, Vector3 up, Vector3 facing)
    {
        m_rigidbody.useGravity = false;
        m_rigidbody.linearVelocity = (targetPosition - m_rigidbody.position) / Time.fixedDeltaTime;
        m_rigidbody.MoveRotation(ComputeRotation(up, facing));
    }

    // Chamar todo FixedUpdate, antes do estado atual
    public void UpdateSurface(Vector2 input)
    {
        if (m_detachTimer > 0f)
        {
            m_detachTimer -= Time.fixedDeltaTime;
            m_rigidbody.useGravity = true;
            m_surfaceFallSpeed = 0f;
            IsGrounded = false;
            RotateAroundColliderCenter(ComputeRotation(Vector3.up, Vector3.zero));
            return;
        }

        SurfaceProbeResult _probeResult = m_surfaceProbe.Probe(m_transform);
        m_normalTracker.Update(_probeResult, Time.fixedDeltaTime);

        IsGrounded = _probeResult.GroundHitFound && _probeResult.GroundDistance <= m_settings.GroundedDistance;
        UpdateSurfaceGravity();

        Vector3 _normal = m_normalTracker.CurrentNormal;
        SurfaceMoveDirection = UtilsToAbilityDraco.NewOrientationPlayer(m_transform, _normal, m_camera, input).normalized;

        bool _hasInput = input.sqrMagnitude >= k_inputDeadZone;
        RotateAroundColliderCenter(ComputeRotation(_normal, _hasInput ? SurfaceMoveDirection : Vector3.zero));

        m_rigidbody.useGravity = !m_normalTracker.HasNormal;
    }

    // Gravidade falsa: acelera contra a superficie ate encostar nela.
    // No grace time (perdeu a superficie por um instante) continua puxando
    // para a ultima normal, e isso que faz ele contornar quinas.
    private void UpdateSurfaceGravity()
    {
        if (!m_normalTracker.HasNormal || m_normalTracker.CurrentDistance <= m_settings.GravityOffDistance)
            m_surfaceFallSpeed = 0f;
        else
            m_surfaceFallSpeed += m_settings.SurfaceGravity * Time.fixedDeltaTime;
    }

    // Igual ao Spider: o up vai suavemente para a normal mantendo o "right" do corpo
    // (vira em pitch/roll sem girar para os lados). Com input, o forward vira
    // para onde anda, usando o novo up.
    private Quaternion ComputeRotation(Vector3 targetNormal, Vector3 facingDirection)
    {
        float _t = 1f - Mathf.Exp(-m_settings.NormalSlerpSpeed * Time.fixedDeltaTime);

        Quaternion _currentRotation = m_rigidbody.rotation;
        Vector3 _currentUp = _currentRotation * Vector3.up;
        Vector3 _up = Vector3.Slerp(_currentUp, targetNormal, _t).normalized;

        Vector3 _right = Vector3.ProjectOnPlane(_currentRotation * Vector3.right, _up);
        Quaternion _targetRotation = _right.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(Vector3.Cross(_right.normalized, _up), _up)
            : Quaternion.FromToRotation(_currentUp, _up) * _currentRotation;

        Vector3 _forward = Vector3.ProjectOnPlane(facingDirection, _up);
        if (_forward.sqrMagnitude > 0.001f)
            _targetRotation = Quaternion.Slerp(_targetRotation, Quaternion.LookRotation(_forward.normalized, _up), _t);

        return _targetRotation;
    }

    // O pivot do Draco fica no pe; girar em volta dele faz o rabo/nariz entrarem
    // no chao/parede quando ele vira para escalar. Gira em volta do centro do collider.
    private void RotateAroundColliderCenter(Quaternion targetRotation)
    {
        // Sem rotacao para aplicar: nao mexe na posicao
        if (Quaternion.Angle(m_rigidbody.rotation, targetRotation) < 0.01f) return;

        Vector3 _localCenter = m_collider != null ? Vector3.Scale(m_collider.center, m_transform.lossyScale) : Vector3.zero;
        Vector3 _worldCenter = m_rigidbody.position + m_rigidbody.rotation * _localCenter;

        m_rigidbody.MoveRotation(targetRotation);
        m_rigidbody.MovePosition(_worldCenter - targetRotation * _localCenter);
    }
}
