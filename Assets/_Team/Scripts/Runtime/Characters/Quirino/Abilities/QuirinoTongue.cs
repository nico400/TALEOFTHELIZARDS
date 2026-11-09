using UnityEngine;

// Dados e visual da lingua do Quirino, compartilhados pelos estados de lingua.
// Nao decide nada: quem decide quando esticar, prender e recolher sao os estados.
public class QuirinoTongue : ITongueBridge
{
    private const float k_cameraAimDistance = 100f;
    private const float k_reachedDistance = 0.05f;
    private const float k_lineOfSightPadding = 0.05f;
    private const int k_maxAssistHits = 16;

    private readonly Transform m_origin;
    private readonly LineRenderer m_line;
    private readonly Transform m_camera;
    private readonly QuirinoSettings m_settings;
    private readonly LayerMask m_layerMask;
    private readonly RaycastHit[] m_assistHits = new RaycastHit[k_maxAssistHits];

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

        // 1) Raio exato: se ja acerta um alvo, usa ele
        bool _hasExactHit = Physics.Raycast(_origin, _direction, out RaycastHit _exactHit, m_settings.TongueRange, m_layerMask);
        ITongueTarget _exactTarget = _hasExactHit ? _exactHit.collider.GetComponentInParent<ITongueTarget>() : null;

        if (_exactTarget != null)
            SetHit(_exactHit.transform, _exactHit.point, _exactTarget);
        // 2) Ajuda de mira: SphereCast procura o alvo mais alinhado com a mira
        else if (TryPredictTarget(_origin, _direction, out Transform _assistTransform, out Vector3 _assistPoint, out ITongueTarget _assistTarget))
            SetHit(_assistTransform, _assistPoint, _assistTarget);
        // 3) Nenhum alvo: gruda na superficie que o raio exato acertou (ou erra)
        else if (_hasExactHit)
            SetHit(_exactHit.transform, _exactHit.point, null);

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

    private void SetHit(Transform hitTransform, Vector3 point, ITongueTarget target)
    {
        m_hitTransform = hitTransform;
        m_localHitPoint = hitTransform.InverseTransformPoint(point);
        Target = target;
    }

    // Varre um "tubo" em volta da mira e escolhe o alvo com menor angulo ate ela,
    // desde que nada esteja entre a boca e o alvo
    private bool TryPredictTarget(Vector3 origin, Vector3 direction, out Transform hitTransform, out Vector3 point, out ITongueTarget target)
    {
        hitTransform = null;
        point = default;
        target = null;

        if (m_settings.TongueAimAssistRadius <= 0f) return false;

        int _count = Physics.SphereCastNonAlloc(origin, m_settings.TongueAimAssistRadius, direction,
            m_assistHits, m_settings.TongueRange, m_layerMask);

        float _bestAngle = m_settings.TongueAimAssistMaxAngle;
        for (int i = 0; i < _count; i++)
        {
            RaycastHit _hit = m_assistHits[i];
            ITongueTarget _target = _hit.collider.GetComponentInParent<ITongueTarget>();
            if (_target == null) continue;

            // Collider que ja comeca dentro da esfera vem com point zerado
            Vector3 _point = _hit.distance <= 0f ? _hit.collider.ClosestPoint(origin) : _hit.point;
            Vector3 _toPoint = _point - origin;
            float _distance = _toPoint.magnitude;
            if (_distance <= Mathf.Epsilon || _distance > m_settings.TongueRange) continue;

            float _angle = Vector3.Angle(direction, _toPoint);
            if (_angle > _bestAngle) continue;
            if (!HasLineOfSight(origin, _toPoint / _distance, _distance, _target)) continue;

            _bestAngle = _angle;
            hitTransform = _hit.transform;
            point = _point;
            target = _target;
        }

        return target != null;
    }

    // O raio da boca ate o ponto so pode bater no proprio alvo
    private bool HasLineOfSight(Vector3 origin, Vector3 direction, float distance, ITongueTarget target)
    {
        if (!Physics.Raycast(origin, direction, out RaycastHit _hit, distance + k_lineOfSightPadding, m_layerMask))
            return true;

        return _hit.collider.GetComponentInParent<ITongueTarget>() == target;
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
