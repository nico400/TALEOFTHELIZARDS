using System;
using System.Collections.Generic;
using UnityEngine;

// Ponto flutuante em cima de objetos interagiveis, so para mostrar que da para usar.
// Aparece quando o Draco ou o Quirino entra na area dele, fica sempre virado para a
// camera e e desenhado por cima de tudo (nada esconde, nem o proprio objeto).
// Interactable e Grabbable criam um sozinhos e dizem o tamanho da area (no Inspector deles);
// adicione no objeto a mao so para mudar imagem, cor, tamanho ou altura.
[DisallowMultipleComponent]
public class InteractionIndicator : MonoBehaviour
{
    private const string k_shaderResource = "InteractionIndicator";

    [Tooltip("Imagem do ponto. Vazio = circulo branco gerado no jogo")]
    [SerializeField] private Texture2D m_texture;
    [SerializeField] private Color m_color = Color.white;
    [Tooltip("Tamanho no mundo, nao depende da escala do objeto")]
    [SerializeField, Min(0f)] private float m_size = 0.15f;

    [Tooltip("Calcula a altura pelo topo dos renderers do objeto")]
    [SerializeField] private bool m_autoHeight = true;
    [Tooltip("Distancia acima do topo do objeto (ou acima do pivo, sem altura automatica)")]
    [SerializeField] private float m_height = 0.3f;

    [SerializeField, Min(0f)] private float m_bobAmplitude = 0.05f;
    [SerializeField, Min(0f)] private float m_bobSpeed = 2f;

    // Quem criou o indicador diz quando ele pode aparecer (ex: escondido enquanto interage)
    public Func<bool> ShouldShow { get; set; }

    // Area: distancia de um personagem ate AreaCenter para o ponto aparecer
    public float ShowDistance { get; set; } = 4f;
    public Transform AreaCenter { get; set; }

    // Personagens procurados na cena; refeito quando algum some
    private static readonly List<MonoBehaviour> s_players = new();
    private static float s_nextPlayerSearch;

    private static Texture2D s_defaultTexture;
    private static Material s_material;
    private static Mesh s_quad;
    private static MaterialPropertyBlock s_properties;

    private Transform m_dot;
    private MeshRenderer m_renderer;
    private float m_heightAbovePivot;
    private float m_bobOffset;

    // Pega o indicador do objeto ou cria um com os valores padrao
    public static InteractionIndicator Ensure(GameObject target)
    {
        return target.TryGetComponent(out InteractionIndicator indicator)
            ? indicator : target.AddComponent<InteractionIndicator>();
    }

    private void Awake()
    {
        Material material = SharedMaterial();
        if (material == null)
        {
            Debug.LogError($"Shader do InteractionIndicator nao encontrado em Resources/{k_shaderResource}", this);
            enabled = false;
            return;
        }

        m_heightAbovePivot = m_height;
        if (m_autoHeight) m_heightAbovePivot += TopAbovePivot();

        var dot = new GameObject("InteractionIndicator");
        m_dot = dot.transform;
        m_dot.SetParent(transform, false);

        dot.AddComponent<MeshFilter>().sharedMesh = SharedQuad();
        m_renderer = dot.AddComponent<MeshRenderer>();
        m_renderer.sharedMaterial = material;
        m_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_renderer.receiveShadows = false;
        m_renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        m_renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        m_renderer.enabled = false;

        s_properties ??= new MaterialPropertyBlock();
        s_properties.Clear();
        s_properties.SetTexture("_MainTex", m_texture != null ? m_texture : DefaultTexture());
        s_properties.SetColor("_Color", m_color);
        m_renderer.SetPropertyBlock(s_properties);

        // Cada indicador balanca num tempo diferente
        m_bobOffset = UnityEngine.Random.value * Mathf.PI * 2f;
    }

    private void OnEnable() { if (m_dot != null) m_dot.gameObject.SetActive(true); }

    private void OnDisable() { if (m_dot != null) m_dot.gameObject.SetActive(false); }

    private void OnDestroy()
    {
        if (m_dot != null) Destroy(m_dot.gameObject);
    }

    private void LateUpdate()
    {
        bool visible = (ShouldShow == null || ShouldShow()) && IsPlayerInArea();
        m_renderer.enabled = visible;
        if (!visible) return;

        float bob = Mathf.Sin(Time.time * m_bobSpeed + m_bobOffset) * m_bobAmplitude;
        m_dot.position = transform.position + Vector3.up * (m_heightAbovePivot + bob);

        // Tamanho fixo no mundo mesmo com o objeto escalado; o shader vira para a camera
        Vector3 parentScale = transform.lossyScale;
        m_dot.localScale = new Vector3(
            m_size / NonZero(parentScale.x), m_size / NonZero(parentScale.y), m_size / NonZero(parentScale.z));
    }

    private bool IsPlayerInArea()
    {
        List<MonoBehaviour> players = Players();
        float sqrDistance = ShowDistance * ShowDistance;
        Vector3 center = AreaCenter != null ? AreaCenter.position : transform.position;

        for (int i = 0; i < players.Count; i++)
        {
            MonoBehaviour player = players[i];
            if (player == null || !player.isActiveAndEnabled) continue;
            if ((player.transform.position - center).sqrMagnitude <= sqrDistance) return true;
        }

        return false;
    }

    private static List<MonoBehaviour> Players()
    {
        bool missing = s_players.Count == 0;
        for (int i = 0; i < s_players.Count && !missing; i++)
            missing = s_players[i] == null;

        // Procurar na cena e caro: so de novo quando falta alguem, e no maximo 1x por segundo
        if (missing && Time.unscaledTime >= s_nextPlayerSearch)
        {
            s_nextPlayerSearch = Time.unscaledTime + 1f;
            s_players.Clear();
            s_players.AddRange(FindObjectsByType<ControllerDraco>(FindObjectsSortMode.None));
            s_players.AddRange(FindObjectsByType<ControllerQuirino>(FindObjectsSortMode.None));
        }

        return s_players;
    }

    private float TopAbovePivot()
    {
        bool found = false;
        float top = 0f;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            top = found ? Mathf.Max(top, r.bounds.max.y) : r.bounds.max.y;
            found = true;
        }
        return found ? Mathf.Max(0f, top - transform.position.y) : 0f;
    }

    private static float NonZero(float value) => Mathf.Abs(value) < 0.0001f ? 0.0001f : value;

    private static Material SharedMaterial()
    {
        if (s_material != null) return s_material;

        var shader = Resources.Load<Shader>(k_shaderResource);
        if (shader == null) return null;

        s_material = new Material(shader) { name = "InteractionIndicator" };
        return s_material;
    }

    // Quad de 1x1 centrado; quem vira para a camera e o shader
    private static Mesh SharedQuad()
    {
        if (s_quad != null) return s_quad;

        s_quad = new Mesh { name = "InteractionIndicatorQuad" };
        s_quad.SetVertices(new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
        });
        s_quad.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
        s_quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
        // Bounds grandes: o shader move os cantos, nao deixa a camera cortar o ponto
        s_quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        return s_quad;
    }

    // Circulo branco com borda suave, criado uma vez e usado por todos
    private static Texture2D DefaultTexture()
    {
        if (s_defaultTexture != null) return s_defaultTexture;

        const int size = 64;
        s_defaultTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "InteractionIndicatorDot",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        float radius = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                byte alpha = (byte)(Mathf.Clamp01(radius - 1f - dist) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        s_defaultTexture.SetPixels32(pixels);
        s_defaultTexture.Apply(false, true);
        return s_defaultTexture;
    }
}
