using UnityEngine;

// Draco andando em cima da lingua do Quirino como se fosse um trilho:
// so vai em direcao a ponta ou a boca, para nas duas pontas,
// sai com o pulo e cai com gravidade normal se a lingua recolher.
public class TongueWalkStateDraco : State<ControllerDraco>
{
    private const float k_inputDeadZone = 0.1f;
    private const float k_minBridgeLength = 0.01f;

    private float m_distance;    // distancia da boca ate o Draco, ao longo da lingua
    private float m_facing = 1f; // 1 = olhando para a ponta, -1 = para a boca

    public TongueWalkStateDraco(ControllerDraco controller) : base(controller) { }

    // Lingua presa em alvo caminhavel e perto o bastante do Draco
    public bool CanMount()
    {
        ITongueBridge _bridge = Controller.TongueBridge;
        if (_bridge == null || !_bridge.IsWalkable) return false;

        Vector3 _position = Controller.RB.position;
        Vector3 _closest = PointOnBridge(_bridge, ProjectOnBridge(_bridge, _position));
        return Vector3.Distance(_position, _closest) <= Controller.Settings.TongueMountDistance;
    }

    public override void Enter()
    {
        ITongueBridge _bridge = Controller.TongueBridge;
        m_distance = ProjectOnBridge(_bridge, Controller.RB.position);

        // Continua olhando para o lado que ja estava olhando
        Vector3 _dir = (_bridge.BridgeEnd - _bridge.BridgeStart).normalized;
        m_facing = Vector3.Dot(Controller.transform.forward, _dir) >= 0f ? 1f : -1f;

        Controller.Motor.ClearSurface();
        Controller.JumpBuffer.Consume(); // pulo apertado antes de subir nao derruba na hora
    }

    public override void Update()
    {
        // Lingua recolheu ou o alvo sumiu: cai
        if (!IsBridgeWalkable())
        {
            Controller.SwitchState(Controller.LocomotionState);
            return;
        }

        if (Controller.JumpBuffer.HasBufferedInput)
            Controller.SwitchState(Controller.JumpState);
    }

    public override void FixedUpdate()
    {
        if (!IsBridgeWalkable()) return; // o Update troca de estado

        ITongueBridge _bridge = Controller.TongueBridge;
        Vector3 _start = _bridge.BridgeStart;
        Vector3 _toEnd = _bridge.BridgeEnd - _start;
        float _length = _toEnd.magnitude;
        if (_length < k_minBridgeLength) return;

        Vector3 _dir = _toEnd / _length;
        Vector3 _up = GetBridgeUp(_dir);

        float _input = GetInputAlongBridge(_dir);
        if (Mathf.Abs(_input) >= k_inputDeadZone)
            m_facing = Mathf.Sign(_input);

        // Recalcula o clamp todo frame porque a lingua pode mudar de tamanho
        m_distance = ClampToBridge(m_distance + _input * Controller.Settings.TongueWalkSpeed * Time.fixedDeltaTime, _length);

        Vector3 _target = _start + _dir * m_distance + _up * Controller.Settings.TongueStandHeight;
        Controller.Motor.FollowPoint(_target, _up, _dir * m_facing);
    }

    public override void Exit()
    {
        Controller.RB.useGravity = true;
    }

    private bool IsBridgeWalkable()
    {
        ITongueBridge _bridge = Controller.TongueBridge;
        return _bridge != null && _bridge.IsWalkable;
    }

    // Quanto do input (relativo a camera) aponta para a ponta: +1 = ponta, -1 = boca
    private float GetInputAlongBridge(Vector3 bridgeDir)
    {
        Vector3 _flatDir = Vector3.ProjectOnPlane(bridgeDir, Vector3.up);

        // Lingua quase vertical: frente do analogico sobe em direcao a ponta
        if (_flatDir.sqrMagnitude < 0.01f)
            return Controller.PlayerInput.Move.y * Mathf.Sign(bridgeDir.y);

        Vector3 _worldInput = CameraRelative.ToWorld(Controller.PlayerInput.Move, Controller.Camera);
        return Vector3.Dot(_worldInput, _flatDir.normalized);
    }

    // "Cima" da lingua: o mais perto do up do mundo que fica perpendicular a ela
    private Vector3 GetBridgeUp(Vector3 bridgeDir)
    {
        Vector3 _up = Vector3.ProjectOnPlane(Vector3.up, bridgeDir);
        if (_up.sqrMagnitude < 0.001f)
            _up = Vector3.ProjectOnPlane(Controller.transform.up, bridgeDir);

        return _up.normalized;
    }

    private float ProjectOnBridge(ITongueBridge bridge, Vector3 point)
    {
        Vector3 _toEnd = bridge.BridgeEnd - bridge.BridgeStart;
        float _length = _toEnd.magnitude;
        if (_length < k_minBridgeLength) return 0f;

        float _distance = Vector3.Dot(point - bridge.BridgeStart, _toEnd / _length);
        return ClampToBridge(_distance, _length);
    }

    private Vector3 PointOnBridge(ITongueBridge bridge, float distance)
    {
        Vector3 _dir = (bridge.BridgeEnd - bridge.BridgeStart).normalized;
        return bridge.BridgeStart + _dir * distance;
    }

    // Para antes da boca e da ponta (lingua curta: fica no meio)
    private float ClampToBridge(float distance, float length)
    {
        float _margin = Mathf.Min(Controller.Settings.TongueEdgeMargin, length * 0.5f);
        return Mathf.Clamp(distance, _margin, length - _margin);
    }
}
