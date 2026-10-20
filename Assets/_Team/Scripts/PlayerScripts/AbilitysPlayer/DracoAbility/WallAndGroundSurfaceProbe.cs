using UnityEngine;

// Le a superficie em volta do personagem com dois SphereCasts, no estilo do
// Unity-Procedural-IK-Wall-Walking-Spider (PhilS94):
//  - "parede": do centro do collider para o forward do CORPO (nao do input)
//  - "chao":   do centro do collider para o -up do CORPO
// Os dois sempre rodam. A distancia ate a superficie e medida a partir da
// casca do collider (centro ate o ponto - raio), entao 0 = encostado.
public class WallAndGroundSurfaceProbe
{
    // Parametros de um SphereCast, guardados para o debug visual
    private struct CastInfo
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Radius;
        public float Distance;
        public bool HitFound;
        public RaycastHit Hit;
    }

    private readonly DracoSettings m_settings;
    private readonly CapsuleCollider m_collider;
    private readonly LayerMask m_layerToDetect = ~LayerMask.GetMask("Players");

    private CastInfo m_lastWallCast;
    private CastInfo m_lastGroundCast;
    private bool m_hasProbed;

    public WallAndGroundSurfaceProbe(DracoSettings settings, CapsuleCollider collider)
    {
        m_settings = settings;
        m_collider = collider;
    }

    public SurfaceProbeResult Probe(Transform playerTransform)
    {
        SurfaceProbeResult _data = new SurfaceProbeResult();

        BuildCasts(playerTransform, out CastInfo _wallCast, out CastInfo _groundCast);
        Vector3 _center = GetColliderCenter(playerTransform);
        float _radius = GetColliderRadius(playerTransform);

        _data.WallHitFound = Cast(ref _wallCast);
        _data.WallHit = _wallCast.Hit;
        _data.WallDistance = _data.WallHitFound ? Vector3.Distance(_center, _data.WallHit.point) - _radius : float.PositiveInfinity;

        _data.GroundHitFound = Cast(ref _groundCast);
        _data.GroundHit = _groundCast.Hit;
        _data.GroundDistance = _data.GroundHitFound ? Vector3.Distance(_center, _data.GroundHit.point) - _radius : float.PositiveInfinity;

        m_lastWallCast = _wallCast;
        m_lastGroundCast = _groundCast;
        m_hasProbed = true;

        return _data;
    }

    private bool Cast(ref CastInfo cast)
    {
        cast.HitFound = Physics.SphereCast(cast.Origin, cast.Radius, cast.Direction, out cast.Hit, cast.Distance, m_layerToDetect, QueryTriggerInteraction.Ignore);
        return cast.HitFound;
    }

    private void BuildCasts(Transform playerTransform, out CastInfo wallCast, out CastInfo groundCast)
    {
        Vector3 _center = GetColliderCenter(playerTransform);

        wallCast = new CastInfo
        {
            Origin = _center,
            Direction = playerTransform.forward,
            Radius = m_settings.ForwardProbeRadius,
            Distance = m_settings.ForwardProbeLength
        };

        groundCast = new CastInfo
        {
            Origin = _center,
            Direction = -playerTransform.up,
            Radius = m_settings.DownProbeRadius,
            Distance = m_settings.DownProbeLength
        };
    }

    private Vector3 GetColliderCenter(Transform playerTransform)
    {
        return m_collider != null ? playerTransform.TransformPoint(m_collider.center) : playerTransform.position;
    }

    private float GetColliderRadius(Transform playerTransform)
    {
        return m_collider != null ? m_collider.radius * playerTransform.lossyScale.y : 0f;
    }

    // Debug visual dos casts. Chamar dentro de um OnDrawGizmos.
    // Fora do play mostra uma previa com a pose atual do transform.
    public void DrawGizmos(Transform playerTransform)
    {
        CastInfo _wallCast = m_lastWallCast;
        CastInfo _groundCast = m_lastGroundCast;

        if (!Application.isPlaying || !m_hasProbed)
            BuildCasts(playerTransform, out _wallCast, out _groundCast);

        // Parede: ciano / acertou = verde
        DrawCast(_wallCast, Color.cyan, Color.green);
        // Chao: amarelo / acertou = vermelho
        DrawCast(_groundCast, Color.yellow, Color.red);
    }

    private static void DrawCast(CastInfo cast, Color rangeColor, Color hitColor)
    {
        Vector3 _end = cast.Origin + cast.Direction * cast.Distance;

        // Inicio, fim (alcance maximo) e o "tubo" que a esfera varre
        Gizmos.color = rangeColor;
        Gizmos.DrawWireSphere(cast.Origin, cast.Radius);
        Gizmos.DrawWireSphere(_end, cast.Radius);
        Gizmos.DrawLine(cast.Origin, _end);

        Vector3 _side = Vector3.Cross(cast.Direction, Vector3.up);
        if (_side.sqrMagnitude < 0.001f) _side = Vector3.Cross(cast.Direction, Vector3.right);
        _side = _side.normalized * cast.Radius;
        Vector3 _sideUp = Vector3.Cross(cast.Direction, _side).normalized * cast.Radius;

        Gizmos.DrawLine(cast.Origin + _side, _end + _side);
        Gizmos.DrawLine(cast.Origin - _side, _end - _side);
        Gizmos.DrawLine(cast.Origin + _sideUp, _end + _sideUp);
        Gizmos.DrawLine(cast.Origin - _sideUp, _end - _sideUp);

        if (!cast.HitFound) return;

        // Onde a esfera parou, o ponto de contato e a normal
        Vector3 _sphereAtHit = cast.Origin + cast.Direction * cast.Hit.distance;
        Gizmos.color = hitColor;
        Gizmos.DrawWireSphere(_sphereAtHit, cast.Radius);
        Gizmos.DrawSphere(cast.Hit.point, 0.03f);
        Gizmos.DrawLine(cast.Hit.point, cast.Hit.point + cast.Hit.normal * 0.3f);
    }
}
