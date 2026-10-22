using UnityEngine;

// Area de deteccao do Quirino: acha o JumpPoint mais alinhado
// com a direcao em que o jogador quer pular.
public class JumpPointDetector
{
    private const int k_maxColliders = 16;

    // Alvos com angulos quase iguais (diferenca menor que isso) desempatam pela distancia
    private const float k_tieAngle = 5f;

    private readonly Transform m_transform;
    private readonly QuirinoSettings m_settings;
    private readonly Collider[] m_hits = new Collider[k_maxColliders];

    // Ultima busca, so para desenhar no gizmo
    public Vector3 LastOrigin { get; private set; }
    public Vector3 LastDirection { get; private set; }
    public JumpPoint LastResult { get; private set; }

    public JumpPointDetector(Transform transform, QuirinoSettings settings)
    {
        m_transform = transform;
        m_settings = settings;
    }

    // direction: para onde o jogador quer pular (so conta o plano do chao)
    // ignore: JumpPoint onde o Quirino ja esta, para nao pular nele mesmo
    public bool TryFind(Vector3 direction, JumpPoint ignore, out JumpPoint best)
    {
        best = null;
        LastOrigin = m_transform.position;
        LastDirection = Vector3.zero;
        LastResult = null;

        Vector3 _direction = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (_direction.sqrMagnitude < 0.001f) return false; // sem direcao: pulo normal
        _direction.Normalize();
        LastDirection = _direction;

        Vector3 _origin = m_transform.position;
        float _radius = m_settings.JumpPointDetectRadius;
        int _count = Physics.OverlapSphereNonAlloc(_origin, _radius, m_hits, m_settings.JumpPointLayers, QueryTriggerInteraction.Collide);

        float _bestAngle = float.MaxValue;
        float _bestSqrDistance = float.MaxValue;

        for (int i = 0; i < _count; i++)
        {
            JumpPoint _point = m_hits[i].GetComponentInParent<JumpPoint>();
            if (_point == null || _point == ignore || !_point.IsAvailable) continue;

            // O Target tem que estar dentro da area, nao so o collider do objeto
            Vector3 _toTarget = _point.LandPosition - _origin;
            float _sqrDistance = _toTarget.sqrMagnitude;
            if (_sqrDistance > _radius * _radius) continue;

            Vector3 _flatToTarget = Vector3.ProjectOnPlane(_toTarget, Vector3.up);
            if (_flatToTarget.sqrMagnitude < 0.01f) continue; // logo acima/abaixo: sem direcao

            float _angle = Vector3.Angle(_flatToTarget, _direction);
            if (_angle > m_settings.JumpPointMaxAngle) continue;

            // O mais alinhado ganha; se empatar no angulo, o mais perto
            bool _clearlyBetter = _angle < _bestAngle - k_tieAngle;
            bool _tieButCloser = _angle < _bestAngle + k_tieAngle && _sqrDistance < _bestSqrDistance;
            if (!_clearlyBetter && !_tieButCloser) continue;

            _bestAngle = _angle;
            _bestSqrDistance = _sqrDistance;
            best = _point;
        }

        LastResult = best;
        return best != null;
    }
}
