using UnityEngine;

// Dados e visual da lingua do Quirino, compartilhados pelos estados de lingua.
// Nao decide nada: quem decide quando esticar, prender e recolher sao os estados.
public class QuirinoTongue : ITongueBridge
{
    private const float k_cameraAimDistance = 100f;
    private const float k_reachedDistance = 0.05f;

    private readonly Transform m_origin;
    private readonly LineRenderer m_line;
    private readonly Transform m_camera;
    private readonly QuirinoSettings m_settings;
    private readonly LayerMask m_layerMask;

    // Guarda o ponto no espaco local do objeto para a lingua seguir alvos que se mexem
    private Transform m_hitTransform;
    private Vector3 m_localHitPoint;
    private Vector3 m_missPoint;

    public Vector3 TipPosition { get; private set; }
    public Vector3 OriginPosition => m_origin.position;
    public ITongueTarget Target { get; private set; }

    // Unity "destroi" objetos sem zerar a interface, entao checa pelo Transform tambem
    public bool HasTarget => Target != null && m_hitTransform != null;

    public Vector3 AttachPoint => m_hitTransform != null ? m_hitTransform.TransformPoint(m_localHitPoint) : m_missPoint;

    public bool IsAttachInRange => HasTarget && Vector3.Distance(OriginPosition, AttachPoint) <= m_settings.TongueRange;

    // Fora da boca: de esticar ate terminar de recolher
    public bool IsOut { get; private set; }

    // Ligado pelo TongueAttachedState: so conta como presa depois que a ponta chegou
    public bool IsAttached { get; set; }

    public bool IsWalkable => IsAttached && HasTarget && Target.IsWalkable;
    public Vector3 BridgeStart => OriginPosition;
    public Vector3 BridgeEnd => AttachPoint;

    public QuirinoTongue(Transform origin, LineRenderer line, Transform camera, QuirinoSettings settings)
    {
        m_origin = origin;
        m_line = line;
        m_camera = camera;
        m_settings = settings;
        m_layerMask = ~LayerMask.GetMask("Players");

        if (m_line != null)
        {
            m_line.positionCount = 2;
            m_line.enabled = false;
        }
    }

    // Escolhe para onde a lingua vai: o ponto que o centro da camera esta mirando,
    // limitado pelo alcance a partir da boca
    public void Shoot()
    {
        Vector3 _origin = OriginPosition;
        Vector3 _aimPoint = GetCameraAimPoint();
        Vector3 _direction = (_aimPoint - _origin).normalized;

        Target = null;
        m_hitTransform = null;
        m_missPoint = _origin + _direction * m_settings.TongueRange;

        if (Physics.Raycast(_origin, _direction, out RaycastHit _hit, m_settings.TongueRange, m_layerMask))
        {
            m_hitTransform = _hit.transform;
            m_localHitPoint = _hit.transform.InverseTransformPoint(_hit.point);
            Target = _hit.collider.GetComponentInParent<ITongueTarget>();
        }

        TipPosition = _origin;
        IsOut = true;
        if (m_line != null) m_line.enabled = true;
        Draw();
    }

    // Move a ponta e retorna true quando chegou
    public bool MoveTip(Vector3 destination, float speed)
    {
        TipPosition = Vector3.MoveTowards(TipPosition, destination, speed * Time.fixedDeltaTime);
        return Vector3.Distance(TipPosition, destination) <= k_reachedDistance;
    }

    public void SnapTipToAttachPoint() => TipPosition = AttachPoint;

    // Esquece o alvo (a ponta continua onde esta para voltar dali)
    public void Release()
    {
        IsAttached = false;
        Target = null;
        m_hitTransform = null;
    }

    public void Hide()
    {
        Release();
        IsOut = false;
        if (m_line != null) m_line.enabled = false;
    }

    // Chamar no Update para acompanhar a boca a cada frame
    public void Draw()
    {
        if (m_line == null) return;
        m_line.SetPosition(0, OriginPosition);
        m_line.SetPosition(1, TipPosition);
    }

    // Comeca o raio na altura do Quirino para nao acertar nada entre a camera e ele
    private Vector3 GetCameraAimPoint()
    {
        Vector3 _forward = m_camera.forward;
        float _skip = Mathf.Max(0f, Vector3.Dot(OriginPosition - m_camera.position, _forward));
        Vector3 _start = m_camera.position + _forward * _skip;

        if (Physics.Raycast(_start, _forward, out RaycastHit _hit, k_cameraAimDistance, m_layerMask, QueryTriggerInteraction.Ignore))
            return _hit.point;

        return _start + _forward * k_cameraAimDistance;
    }
}
