using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Abre um furo circular nas paredes (materiais com o shader TaleOfTheLizards/SeeThroughLit)
// quando algo fica entre esta câmera e o player. Cada câmera tem o seu alvo, então funciona em split-screen.
[RequireComponent(typeof(Camera))]
public class SeeThroughWall : MonoBehaviour
{
    private static readonly int s_targetId = Shader.PropertyToID("_SeeThroughTarget");
    private static readonly int s_paramsId = Shader.PropertyToID("_SeeThroughParams");
    private static readonly List<SeeThroughWall> s_active = new();

    [Tooltip("Player que esta câmera segue")]
    [SerializeField] private Transform m_target;
    [Tooltip("Deslocamento a partir do pivô do player (centro do corpo)")]
    [SerializeField] private Vector3 m_targetOffset = new(0f, 0.5f, 0f);

    [Header("Furo")]
    [Tooltip("Raio do círculo em metros, na distância do player")]
    [SerializeField] private float m_radius = 1.5f;
    [Tooltip("Largura da borda suave em metros")]
    [SerializeField] private float m_softness = 0.25f;
    [Tooltip("Só corta o que está pelo menos esta distância na frente do player (evita cortar o chão perto dele)")]
    [SerializeField] private float m_depthOffset = 0.5f;
    [Tooltip("Velocidade de abrir/fechar o furo (aberturas por segundo)")]
    [SerializeField] private float m_openSpeed = 5f;

    [Header("Detecção")]
    [Tooltip("Layers que contam como parede bloqueando a visão")]
    [SerializeField] private LayerMask m_obstacleMask = ~0;
    [SerializeField] private float m_checkRadius = 0.2f;

    private Camera m_camera;
    private float m_amount;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (s_active.Count == 0)
        {
            // Built-in usa Camera.onPreCull; URP/HDRP usam beginCameraRendering
            Camera.onPreCull += OnPreCullCamera;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }
        s_active.Add(this);
    }

    private void OnDisable()
    {
        s_active.Remove(this);
        if (s_active.Count == 0)
        {
            Camera.onPreCull -= OnPreCullCamera;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Shader.SetGlobalVector(s_targetId, Vector4.zero);
        }
    }

    private void LateUpdate()
    {
        float _goal = IsTargetBlocked() ? 1f : 0f;
        m_amount = Mathf.MoveTowards(m_amount, _goal, m_openSpeed * Time.deltaTime);
    }

    private bool IsTargetBlocked()
    {
        if (m_target == null) return false;

        Vector3 _origin = transform.position;
        Vector3 _toTarget = m_target.position + m_targetOffset - _origin;
        float _distance = _toTarget.magnitude - m_checkRadius;
        if (_distance <= 0f) return false;

        // Ignora os colliders do próprio player
        RaycastHit[] _hits = Physics.SphereCastAll(_origin, m_checkRadius, _toTarget.normalized, _distance,
            m_obstacleMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit _hit in _hits)
        {
            if (!_hit.transform.IsChildOf(m_target))
                return true;
        }
        return false;
    }

    private void Apply()
    {
        Vector3 _position = m_target != null ? m_target.position + m_targetOffset : Vector3.zero;
        float _amount = m_target != null ? Mathf.SmoothStep(0f, 1f, m_amount) : 0f;

        Shader.SetGlobalVector(s_targetId, new Vector4(_position.x, _position.y, _position.z, _amount));
        Shader.SetGlobalVector(s_paramsId, new Vector4(m_radius, m_softness, m_depthOffset, 0f));
    }

    private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) => ApplyFor(camera);
    private static void OnPreCullCamera(Camera camera) => ApplyFor(camera);

    // Os globais são trocados antes de cada câmera; câmeras sem este componente (scene view etc.) ficam sem furo
    private static void ApplyFor(Camera camera)
    {
        foreach (SeeThroughWall _wall in s_active)
        {
            if (_wall.m_camera != camera) continue;
            _wall.Apply();
            return;
        }
        Shader.SetGlobalVector(s_targetId, Vector4.zero);
    }
}
