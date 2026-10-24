using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EditorMeshUtility = UnityEditor.ProBuilder.EditorMeshUtility;
using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

// Pincel de texturas em camadas para objetos do ProBuilder (Tools > Tale of the Lizards > Pintura de Texturas).
// O objeto usa o shader TaleOfTheLizards/LayeredPaint (4 camadas de textura) e o componente LayeredPaintSurface,
// que guarda uma máscara PNG no UV2 do objeto. O pincel é uma esfera no mundo: cada texel da máscara sabe a
// posição dele no objeto, então a pintura passa de uma face para outra sem emendas.
public class LayeredPaintWindow : EditorWindow
{
    private const string k_shaderName = "TaleOfTheLizards/LayeredPaint";
    private const string k_maskFolder = "Assets/_Team/Textures/PaintMasks";
    private const string k_materialFolder = "Assets/_Team/Materials/LayeredPaint";
    private const string k_prefsPrefix = "TOTL.LayeredPaint.";

    private const int k_dilatePasses = 4;       // texels extras em volta das ilhas do UV2 (evita borda escura)
    private const int k_gridResolution = 32;    // grid espacial para achar rápido os texels dentro do pincel
    private const float k_dabSpacing = 0.25f;   // distância entre carimbos, em raios do pincel
    private const float k_dabAmount = 0.5f;     // quanto cada carimbo soma com força 1
    private const long k_undoMemoryBudget = 128L * 1024 * 1024;

    private static readonly string[] s_layerNames = { "Base", "Camada 1", "Camada 2", "Camada 3" };
    private static readonly Color[] s_layerColors = { Color.white, new(1f, 0.45f, 0.4f), new(0.5f, 1f, 0.5f), new(0.45f, 0.65f, 1f) };
    private static readonly int[] s_resolutions = { 256, 512, 1024, 2048 };
    private static readonly string[] s_resolutionLabels = { "256", "512", "1024", "2048" };

    // Pincel (lembrado entre sessões)
    private int m_layer = 1;
    private float m_radius = 1f;
    private float m_strength = 0.5f;
    private float m_hardness = 0.3f;
    private float m_irregularity = 0.5f;
    private int m_maskResolution = 1024;

    // Alvo selecionado
    private GameObject m_target;
    private MeshFilter m_filter;
    private MeshRenderer m_renderer;
    private LayeredPaintSurface m_surface;

    // Máscara em edição: 3 floats por texel (camadas 1, 2 e 3), espelhados em m_pixels para mandar para a GPU
    private bool m_painting;
    private int m_width;
    private int m_height;
    private float[] m_channels;
    private Color32[] m_pixels;
    private Texture2D m_workingTexture;
    private bool m_unsaved;
    private bool m_hasDirty;
    private int m_dirtyMinX, m_dirtyMinY, m_dirtyMaxX, m_dirtyMaxY;

    // Texels que cobrem a malha, com a posição de cada um no mundo, organizados num grid
    private Vector3[] m_texelPositions;
    private int[] m_texelIndices;
    private int[] m_cellStart;
    private int[] m_cellTexels;
    private Vector3 m_gridMin;
    private Vector3 m_cellSize;
    private int m_meshSignature;

    // Triângulos em espaço de mundo para o raycast do mouse
    private Vector3[] m_worldVertices;
    private int[] m_triangles;

    private bool m_hasHit;
    private Vector3 m_hitPoint;
    private Vector3 m_hitNormal;
    private bool m_stroking;
    private Vector3 m_lastDab;
    private Vector3 m_noiseOffset;

    // Undo: versão -> pixels depois daquela versão
    private LayeredPaintUndoState m_undoState;
    private readonly Dictionary<int, Color32[]> m_history = new();
    private int m_version;
    private int m_versionCounter;

    private Vector2 m_scroll;

    [MenuItem("Tools/Tale of the Lizards/Pintura de Texturas")]
    private static void Open() => GetWindow<LayeredPaintWindow>("Pintura de Texturas");

    private void OnEnable()
    {
        LoadPrefs();
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        RefreshTarget();
    }

    private void OnDisable()
    {
        StopPainting();
        SavePrefs();
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
    }

    private void OnSelectionChanged()
    {
        if (m_painting && Selection.activeGameObject != m_target)
            StopPainting();

        RefreshTarget();
        Repaint();
    }

    private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path) => SaveMask();

    private void RefreshTarget()
    {
        m_target = Selection.activeGameObject;
        m_filter = m_target != null ? m_target.GetComponent<MeshFilter>() : null;
        m_renderer = m_target != null ? m_target.GetComponent<MeshRenderer>() : null;
        m_surface = m_target != null ? m_target.GetComponent<LayeredPaintSurface>() : null;
    }

    // ---------------------------------------------------------------------------------------------
    // Janela
    // ---------------------------------------------------------------------------------------------

    private void OnGUI()
    {
        m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

        if (m_target == null || m_filter == null || m_renderer == null || m_filter.sharedMesh == null)
        {
            EditorGUILayout.HelpBox("Selecione na cena um objeto do ProBuilder (ou qualquer objeto com MeshRenderer).", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField(m_target.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        bool _ready = DrawSetupSteps();
        if (_ready)
        {
            EditorGUILayout.Space(8);
            DrawPaintToggle();
            EditorGUILayout.Space(8);
            DrawLayerPicker();
            EditorGUILayout.Space(8);
            DrawBrushSettings();

            if (m_painting)
            {
                EditorGUILayout.Space(8);
                DrawActions();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Clique e arraste na Scene para pintar.\n" +
                "Shift: apaga a camada selecionada  |  1 a 4: troca a camada\n" +
                "[ e ] ou Ctrl + scroll: tamanho do pincel  |  Ctrl+Z: desfaz  |  Esc: para de pintar\n" +
                "As texturas, tamanhos e o anti-repetição de cada camada ficam no material.",
                MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    // Material com o shader certo -> UV2 -> máscara. Retorna true quando dá para pintar
    private bool DrawSetupSteps()
    {
        Material _material = m_renderer.sharedMaterial;
        bool _hasShader = _material != null && _material.shader != null && _material.shader.name == k_shaderName;

        EditorGUI.BeginChangeCheck();
        Material _newMaterial = (Material)EditorGUILayout.ObjectField("Material de camadas", _material, typeof(Material), false);
        if (EditorGUI.EndChangeCheck() && _newMaterial != null)
            AssignMaterial(_newMaterial);

        if (!_hasShader)
        {
            EditorGUILayout.HelpBox("1. O objeto precisa de um material com o shader " + k_shaderName + ". " +
                                    "Crie um novo (a textura atual vira a camada base) ou arraste um que já existe no campo acima.",
                MessageType.Warning);
            if (GUILayout.Button("Criar material de camadas", GUILayout.Height(28)))
                CreateMaterial();
            return false;
        }

        if (!HasUV2())
        {
            EditorGUILayout.HelpBox("2. O objeto precisa de UV2 (é onde a pintura é guardada). " +
                                    "Para ProBuilder, a ferramenta marca o objeto como Contribute GI para o UV2 ser refeito sozinho quando você editar a malha.",
                MessageType.Warning);
            if (GUILayout.Button("Gerar UV2", GUILayout.Height(28)))
                GenerateUV2();
            return false;
        }

        if (m_surface == null || m_surface.PaintMask == null)
        {
            EditorGUILayout.HelpBox("3. Crie a máscara de pintura deste objeto. Objetos grandes pedem resolução maior.", MessageType.Warning);
            m_maskResolution = EditorGUILayout.IntPopup("Resolução", m_maskResolution, s_resolutionLabels, s_resolutions);
            if (GUILayout.Button("Criar máscara de pintura", GUILayout.Height(28)))
                CreateMask();
            return false;
        }

        if (m_target.GetComponent<ProBuilderMesh>() != null)
            EditorGUILayout.HelpBox("Pinte depois de terminar a modelagem: se mudar a geometria, o UV2 é refeito e a pintura sai do lugar.", MessageType.None);

        return true;
    }

    private void DrawPaintToggle()
    {
        Color _previous = GUI.backgroundColor;
        GUI.backgroundColor = m_painting ? new Color(1f, 0.6f, 0.4f) : new Color(0.5f, 0.9f, 0.5f);
        string _label = m_painting ? (m_unsaved ? "Parar e salvar *" : "Parar de pintar") : "Começar a pintar";
        if (GUILayout.Button(_label, GUILayout.Height(36)))
        {
            if (m_painting) StopPainting();
            else StartPainting();
        }
        GUI.backgroundColor = _previous;
    }

    private void DrawLayerPicker()
    {
        EditorGUILayout.LabelField("Camada do pincel", EditorStyles.boldLabel);
        Material _material = m_renderer.sharedMaterial;

        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < 4; i++)
        {
            Rect _rect = GUILayoutUtility.GetRect(60, 78, GUILayout.ExpandWidth(true));
            Rect _thumb = new(_rect.x + 2, _rect.y + 2, _rect.width - 4, _rect.height - 20);

            if (i == m_layer)
                EditorGUI.DrawRect(_rect, s_layerColors[i]);

            Texture _texture = _material != null ? _material.GetTexture($"_Layer{i}Map") : null;
            if (_texture != null) GUI.DrawTexture(_thumb, _texture, ScaleMode.ScaleAndCrop);
            else EditorGUI.DrawRect(_thumb, new Color(0.25f, 0.25f, 0.25f));

            GUI.Label(new Rect(_rect.x, _rect.yMax - 18, _rect.width, 18), $"{i + 1}. {s_layerNames[i]}",
                i == m_layer ? EditorStyles.boldLabel : EditorStyles.label);

            if (GUI.Button(_rect, GUIContent.none, GUIStyle.none))
                m_layer = i;
        }
        EditorGUILayout.EndHorizontal();

        if (m_layer == 0)
            EditorGUILayout.HelpBox("Pintar a Base apaga todas as camadas por cima dela.", MessageType.None);
    }

    private void DrawBrushSettings()
    {
        EditorGUILayout.LabelField("Pincel", EditorStyles.boldLabel);
        m_radius = EditorGUILayout.Slider(new GUIContent("Raio (metros)"), m_radius, 0.05f, 20f);
        m_strength = EditorGUILayout.Slider(new GUIContent("Força", "Quanto cada passada cobre"), m_strength, 0.01f, 1f);
        m_hardness = EditorGUILayout.Slider(new GUIContent("Dureza", "0 = borda bem suave, 1 = borda dura"), m_hardness, 0f, 0.95f);
        m_irregularity = EditorGUILayout.Slider(new GUIContent("Irregularidade",
            "Deixa a pincelada manchada (ruído), cada passada com um desenho diferente"), m_irregularity, 0f, 1f);
    }

    private void DrawActions()
    {
        EditorGUILayout.LabelField("Ações", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"Preencher tudo com {s_layerNames[m_layer]}"))
            Fill(m_layer, "Preencher pintura");
        if (GUILayout.Button("Limpar pintura"))
            Fill(0, "Limpar pintura");
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!m_unsaved))
        {
            if (GUILayout.Button("Salvar agora"))
                SaveMask();
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Preparação do objeto
    // ---------------------------------------------------------------------------------------------

    private void AssignMaterial(Material material)
    {
        Undo.RecordObject(m_renderer, "Trocar material");
        Material[] _materials = m_renderer.sharedMaterials;
        if (_materials.Length == 0) _materials = new Material[1];
        for (int i = 0; i < _materials.Length; i++)
            _materials[i] = material;
        m_renderer.sharedMaterials = _materials;
    }

    private void CreateMaterial()
    {
        Shader _shader = Shader.Find(k_shaderName);
        if (_shader == null)
        {
            Debug.LogError($"Shader {k_shaderName} não encontrado.");
            return;
        }

        Material _material = new(_shader);
        MaterialEditor.ApplyMaterialPropertyDrawers(_material); // liga a keyword do toggle "Usar normal maps"
        Material _current = m_renderer.sharedMaterial;
        if (_current != null)
        {
            // A textura que o objeto já tinha vira a camada base
            Texture _base = _current.HasProperty("_BaseMap") ? _current.GetTexture("_BaseMap") :
                _current.HasProperty("_MainTex") ? _current.GetTexture("_MainTex") : null;
            if (_base != null) _material.SetTexture("_Layer0Map", _base);
            if (_current.HasProperty("_BumpMap") && _current.GetTexture("_BumpMap") != null)
                _material.SetTexture("_Layer0Normal", _current.GetTexture("_BumpMap"));
            if (_current.HasProperty("_BaseColor"))
                _material.SetColor("_Layer0Color", _current.GetColor("_BaseColor"));
        }

        Directory.CreateDirectory(k_materialFolder);
        string _path = AssetDatabase.GenerateUniqueAssetPath($"{k_materialFolder}/{SafeName(m_target.name)}_Camadas.mat");
        AssetDatabase.CreateAsset(_material, _path);
        AssignMaterial(_material);
        EditorGUIUtility.PingObject(_material);
    }

    private bool HasUV2()
    {
        Mesh _mesh = m_filter.sharedMesh;
        return _mesh != null && _mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1);
    }

    private void GenerateUV2()
    {
        ProBuilderMesh _pbMesh = m_target.GetComponent<ProBuilderMesh>();
        if (_pbMesh != null)
        {
            // Com Contribute GI o ProBuilder refaz o UV2 sozinho depois de cada edição na malha
            StaticEditorFlags _flags = GameObjectUtility.GetStaticEditorFlags(m_target);
            if ((_flags & StaticEditorFlags.ContributeGI) == 0)
            {
                Undo.RecordObject(m_target, "Marcar Contribute GI");
                GameObjectUtility.SetStaticEditorFlags(m_target, _flags | StaticEditorFlags.ContributeGI);
            }
            EditorMeshUtility.Optimize(_pbMesh, true);
            EditorUtility.SetDirty(_pbMesh);
            return;
        }

        Mesh _mesh = m_filter.sharedMesh;
        if (AssetDatabase.Contains(_mesh))
        {
            EditorUtility.DisplayDialog("Gerar UV2",
                "Essa malha vem de um arquivo de modelo. Selecione o modelo no Project, marque \"Generate Lightmap UVs\" e clique em Apply.", "Ok");
            return;
        }

        Undo.RecordObject(_mesh, "Gerar UV2");
        Unwrapping.GenerateSecondaryUVSet(_mesh);
    }

    private void CreateMask()
    {
        int _size = m_maskResolution;
        Texture2D _texture = new(_size, _size, TextureFormat.RGBA32, false, true);
        Color32[] _pixels = new Color32[_size * _size];
        for (int i = 0; i < _pixels.Length; i++)
            _pixels[i] = new Color32(0, 0, 0, 255);
        _texture.SetPixels32(_pixels);
        _texture.Apply(false);

        Directory.CreateDirectory(k_maskFolder);
        string _scene = string.IsNullOrEmpty(m_target.scene.name) ? "Cena" : m_target.scene.name;
        string _path = AssetDatabase.GenerateUniqueAssetPath($"{k_maskFolder}/{SafeName(_scene)}_{SafeName(m_target.name)}_Pintura.png");
        File.WriteAllBytes(_path, _texture.EncodeToPNG());
        DestroyImmediate(_texture);

        AssetDatabase.ImportAsset(_path);
        TextureImporter _importer = (TextureImporter)AssetImporter.GetAtPath(_path);
        _importer.sRGBTexture = false;
        _importer.textureCompression = TextureImporterCompression.Uncompressed;
        _importer.mipmapEnabled = true;
        _importer.wrapMode = TextureWrapMode.Clamp;
        _importer.filterMode = FilterMode.Bilinear;
        _importer.alphaSource = TextureImporterAlphaSource.None;
        _importer.npotScale = TextureImporterNPOTScale.None;
        _importer.maxTextureSize = 4096;
        _importer.SaveAndReimport();

        if (m_surface == null)
            m_surface = Undo.AddComponent<LayeredPaintSurface>(m_target);

        Undo.RecordObject(m_surface, "Criar máscara de pintura");
        m_surface.PaintMask = AssetDatabase.LoadAssetAtPath<Texture2D>(_path);
        EditorUtility.SetDirty(m_surface);
    }

    private static string SafeName(string name)
    {
        foreach (char _c in Path.GetInvalidFileNameChars())
            name = name.Replace(_c, '_');
        return name.Replace(' ', '_');
    }

    // ---------------------------------------------------------------------------------------------
    // Início / fim da pintura
    // ---------------------------------------------------------------------------------------------

    private void StartPainting()
    {
        if (m_surface == null || m_surface.PaintMask == null) return;

        string _path = AssetDatabase.GetAssetPath(m_surface.PaintMask);
        Texture2D _loaded = new(2, 2, TextureFormat.RGBA32, false, true);
        if (string.IsNullOrEmpty(_path) || !_loaded.LoadImage(File.ReadAllBytes(_path)))
        {
            DestroyImmediate(_loaded);
            Debug.LogError("Não consegui ler a máscara de pintura (precisa ser um PNG do projeto).", m_surface);
            return;
        }

        m_width = _loaded.width;
        m_height = _loaded.height;
        m_pixels = _loaded.GetPixels32();
        DestroyImmediate(_loaded);

        m_channels = new float[m_width * m_height * 3];
        PixelsToChannels();

        m_workingTexture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, true)
        {
            name = "Pintura (em edição)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        m_workingTexture.SetPixelData(m_pixels, 0);
        m_workingTexture.Apply(false);
        m_surface.Apply(m_workingTexture);

        BuildMeshCache();

        m_undoState = CreateInstance<LayeredPaintUndoState>();
        m_undoState.hideFlags = HideFlags.DontSave;
        m_history.Clear();
        m_version = 0;
        m_versionCounter = 0;
        m_history[0] = (Color32[])m_pixels.Clone();

        m_painting = true;
        m_unsaved = false;
        Tools.hidden = true;
        SceneView.RepaintAll();
    }

    private void StopPainting()
    {
        if (!m_painting) return;

        SaveMask();
        m_painting = false;
        m_stroking = false;
        Tools.hidden = false;

        if (m_surface != null) m_surface.Apply();
        if (m_workingTexture != null) DestroyImmediate(m_workingTexture);
        if (m_undoState != null)
        {
            Undo.ClearUndo(m_undoState);
            DestroyImmediate(m_undoState);
        }

        m_history.Clear();
        m_channels = null;
        m_pixels = null;
        m_texelPositions = null;
        m_texelIndices = null;
        m_cellTexels = null;
        SceneView.RepaintAll();
        Repaint();
    }

    private void SaveMask()
    {
        if (!m_painting || !m_unsaved || m_surface == null || m_surface.PaintMask == null) return;

        UploadDirty();
        string _path = AssetDatabase.GetAssetPath(m_surface.PaintMask);
        File.WriteAllBytes(_path, m_workingTexture.EncodeToPNG());
        AssetDatabase.ImportAsset(_path);
        m_unsaved = false;
        Repaint();
    }

    // ---------------------------------------------------------------------------------------------
    // Mapa texel -> mundo
    // ---------------------------------------------------------------------------------------------

    private void BuildMeshCache()
    {
        Mesh _mesh = m_filter.sharedMesh;
        Matrix4x4 _toWorld = m_filter.transform.localToWorldMatrix;
        Vector3[] _vertices = _mesh.vertices;
        Vector2[] _uv2 = _mesh.uv2;
        m_triangles = _mesh.triangles;

        m_worldVertices = new Vector3[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
            m_worldVertices[i] = _toWorld.MultiplyPoint3x4(_vertices[i]);

        int _w = m_width;
        int _h = m_height;
        Vector3[] _positions = new Vector3[_w * _h];
        bool[] _covered = new bool[_w * _h];
        Vector2 _scale = new(_w, _h);
        Vector2 _half = new(0.5f, 0.5f);

        // Rasteriza cada triângulo no UV2: o centro de cada texel recebe a posição no mundo
        for (int t = 0; t < m_triangles.Length; t += 3)
        {
            int _ia = m_triangles[t], _ib = m_triangles[t + 1], _ic = m_triangles[t + 2];
            Vector2 _a = Vector2.Scale(_uv2[_ia], _scale) - _half;
            Vector2 _b = Vector2.Scale(_uv2[_ib], _scale) - _half;
            Vector2 _c = Vector2.Scale(_uv2[_ic], _scale) - _half;
            float _area = Edge(_a, _b, _c);
            if (Mathf.Abs(_area) < 1e-8f) continue;

            int _minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(_a.x, Mathf.Min(_b.x, _c.x))));
            int _maxX = Mathf.Min(_w - 1, Mathf.CeilToInt(Mathf.Max(_a.x, Mathf.Max(_b.x, _c.x))));
            int _minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(_a.y, Mathf.Min(_b.y, _c.y))));
            int _maxY = Mathf.Min(_h - 1, Mathf.CeilToInt(Mathf.Max(_a.y, Mathf.Max(_b.y, _c.y))));

            Vector3 _wa = m_worldVertices[_ia], _wb = m_worldVertices[_ib], _wc = m_worldVertices[_ic];
            for (int y = _minY; y <= _maxY; y++)
            {
                for (int x = _minX; x <= _maxX; x++)
                {
                    Vector2 _p = new(x, y);
                    float _u = Edge(_b, _c, _p) / _area;
                    float _v = Edge(_c, _a, _p) / _area;
                    float _s = 1f - _u - _v;
                    if (_u < -1e-4f || _v < -1e-4f || _s < -1e-4f) continue;

                    int _index = y * _w + x;
                    _positions[_index] = _wa * _u + _wb * _v + _wc * _s;
                    _covered[_index] = true;
                }
            }
        }

        // Expande as ilhas alguns texels para fora, senão o filtro bilinear puxa preto nas bordas
        for (int _pass = 0; _pass < k_dilatePasses; _pass++)
        {
            bool[] _next = (bool[])_covered.Clone();
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    int _index = y * _w + x;
                    if (_covered[_index]) continue;

                    int _source = -1;
                    if (x > 0 && _covered[_index - 1]) _source = _index - 1;
                    else if (x < _w - 1 && _covered[_index + 1]) _source = _index + 1;
                    else if (y > 0 && _covered[_index - _w]) _source = _index - _w;
                    else if (y < _h - 1 && _covered[_index + _w]) _source = _index + _w;
                    if (_source < 0) continue;

                    _positions[_index] = _positions[_source];
                    _next[_index] = true;
                }
            }
            _covered = _next;
        }

        int _count = 0;
        for (int i = 0; i < _covered.Length; i++)
            if (_covered[i]) _count++;

        m_texelPositions = new Vector3[_count];
        m_texelIndices = new int[_count];
        Bounds _bounds = new(m_worldVertices.Length > 0 ? m_worldVertices[0] : Vector3.zero, Vector3.zero);
        for (int i = 0, k = 0; i < _covered.Length; i++)
        {
            if (!_covered[i]) continue;
            m_texelPositions[k] = _positions[i];
            m_texelIndices[k] = i;
            _bounds.Encapsulate(_positions[i]);
            k++;
        }

        // Grid uniforme (counting sort) para o pincel só olhar os texels perto dele
        _bounds.Expand(0.01f);
        m_gridMin = _bounds.min;
        m_cellSize = _bounds.size / k_gridResolution;
        int _cellCount = k_gridResolution * k_gridResolution * k_gridResolution;
        m_cellStart = new int[_cellCount + 1];
        int[] _cellOf = new int[_count];
        for (int i = 0; i < _count; i++)
        {
            _cellOf[i] = CellIndex(CellCoord(m_texelPositions[i]));
            m_cellStart[_cellOf[i] + 1]++;
        }
        for (int c = 0; c < _cellCount; c++)
            m_cellStart[c + 1] += m_cellStart[c];

        m_cellTexels = new int[_count];
        int[] _cursor = (int[])m_cellStart.Clone();
        for (int i = 0; i < _count; i++)
            m_cellTexels[_cursor[_cellOf[i]]++] = i;

        m_meshSignature = ComputeMeshSignature();
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

    private Vector3Int CellCoord(Vector3 position)
    {
        Vector3 _local = position - m_gridMin;
        return new Vector3Int(
            Mathf.Clamp((int)(_local.x / m_cellSize.x), 0, k_gridResolution - 1),
            Mathf.Clamp((int)(_local.y / m_cellSize.y), 0, k_gridResolution - 1),
            Mathf.Clamp((int)(_local.z / m_cellSize.z), 0, k_gridResolution - 1));
    }

    private static int CellIndex(Vector3Int cell) => (cell.z * k_gridResolution + cell.y) * k_gridResolution + cell.x;

    // Muda quando a malha é editada ou o objeto é movido: aí o mapa texel -> mundo precisa ser refeito
    private int ComputeMeshSignature()
    {
        Mesh _mesh = m_filter.sharedMesh;
        if (_mesh == null) return 0;

        unchecked
        {
            int _hash = _mesh.GetInstanceID();
            _hash = _hash * 31 + _mesh.vertexCount;
            _hash = _hash * 31 + m_filter.transform.localToWorldMatrix.GetHashCode();

            Vector3 _positionSum = Vector3.zero;
            foreach (Vector3 _vertex in _mesh.vertices) _positionSum += _vertex;
            Vector2 _uvSum = Vector2.zero;
            foreach (Vector2 _uv in _mesh.uv2) _uvSum += _uv;

            _hash = _hash * 31 + _positionSum.GetHashCode();
            _hash = _hash * 31 + _uvSum.GetHashCode();
            return _hash;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Scene View
    // ---------------------------------------------------------------------------------------------

    private void OnSceneGUI(SceneView view)
    {
        if (!m_painting) return;
        if (m_surface == null || m_filter == null || m_filter.sharedMesh == null)
        {
            StopPainting();
            return;
        }

        Event _event = Event.current;
        int _controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (_event.type == EventType.Layout)
            HandleUtility.AddDefaultControl(_controlId);

        switch (_event.type)
        {
            case EventType.KeyDown:
                HandleShortcuts(_event);
                break;

            case EventType.ScrollWheel when _event.control:
                m_radius = Mathf.Clamp(m_radius * (_event.delta.y > 0 ? 0.9f : 1.1f), 0.05f, 20f);
                _event.Use();
                Repaint();
                break;

            case EventType.MouseMove:
                UpdateHit(_event.mousePosition);
                view.Repaint();
                break;

            case EventType.MouseDown when _event.button == 0 && !_event.alt:
                if (ComputeMeshSignature() != m_meshSignature)
                {
                    if (!HasUV2())
                    {
                        Debug.LogWarning("O objeto perdeu o UV2 (a malha foi editada). Gere o UV2 de novo na janela de pintura.", m_target);
                        StopPainting();
                        break;
                    }
                    BuildMeshCache();
                }

                if (UpdateHit(_event.mousePosition))
                {
                    GUIUtility.hotControl = _controlId;
                    m_stroking = true;
                    m_noiseOffset = new Vector3(Random.value, Random.value, Random.value) * 1000f;
                    Dab(m_hitPoint, _event.shift);
                    m_lastDab = m_hitPoint;
                    UploadDirty();
                }
                _event.Use();
                break;

            case EventType.MouseDrag when m_stroking:
                if (UpdateHit(_event.mousePosition))
                {
                    // Carimbos espaçados ao longo do caminho, para a força não depender da velocidade do mouse
                    float _spacing = Mathf.Max(m_radius * k_dabSpacing, 0.001f);
                    float _distance = Vector3.Distance(m_lastDab, m_hitPoint);
                    int _steps = Mathf.Min(Mathf.FloorToInt(_distance / _spacing), 200);
                    Vector3 _from = m_lastDab;
                    for (int i = 1; i <= _steps; i++)
                    {
                        m_lastDab = Vector3.Lerp(_from, m_hitPoint, i * _spacing / _distance);
                        Dab(m_lastDab, _event.shift);
                    }
                    UploadDirty();
                }
                _event.Use();
                break;

            case EventType.MouseUp when m_stroking && _event.button == 0:
                m_stroking = false;
                GUIUtility.hotControl = 0;
                CommitChange(_event.shift ? "Apagar pintura" : "Pintar");
                _event.Use();
                break;

            case EventType.Repaint:
                DrawBrush();
                break;
        }

        if (m_painting)
            DrawSceneInfo();
    }

    private void HandleShortcuts(Event e)
    {
        switch (e.keyCode)
        {
            case KeyCode.Alpha1: m_layer = 0; break;
            case KeyCode.Alpha2: m_layer = 1; break;
            case KeyCode.Alpha3: m_layer = 2; break;
            case KeyCode.Alpha4: m_layer = 3; break;
            case KeyCode.LeftBracket: m_radius = Mathf.Max(0.05f, m_radius * 0.85f); break;
            case KeyCode.RightBracket: m_radius = Mathf.Min(20f, m_radius * 1.15f); break;
            case KeyCode.Escape: StopPainting(); break;
            default: return;
        }
        e.Use();
        Repaint();
    }

    private bool UpdateHit(Vector2 mousePosition)
    {
        Ray _ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        m_hasHit = Raycast(_ray, out m_hitPoint, out m_hitNormal);
        return m_hasHit;
    }

    // Möller–Trumbore em todos os triângulos (malhas do ProBuilder são leves)
    private bool Raycast(Ray ray, out Vector3 point, out Vector3 normal)
    {
        point = default;
        normal = Vector3.up;
        float _best = float.MaxValue;

        for (int t = 0; t < m_triangles.Length; t += 3)
        {
            Vector3 _a = m_worldVertices[m_triangles[t]];
            Vector3 _edge1 = m_worldVertices[m_triangles[t + 1]] - _a;
            Vector3 _edge2 = m_worldVertices[m_triangles[t + 2]] - _a;
            Vector3 _p = Vector3.Cross(ray.direction, _edge2);
            float _det = Vector3.Dot(_edge1, _p);
            if (Mathf.Abs(_det) < 1e-8f) continue;

            float _invDet = 1f / _det;
            Vector3 _s = ray.origin - _a;
            float _u = Vector3.Dot(_s, _p) * _invDet;
            if (_u < 0f || _u > 1f) continue;

            Vector3 _q = Vector3.Cross(_s, _edge1);
            float _v = Vector3.Dot(ray.direction, _q) * _invDet;
            if (_v < 0f || _u + _v > 1f) continue;

            float _distance = Vector3.Dot(_edge2, _q) * _invDet;
            if (_distance <= 0f || _distance >= _best) continue;

            _best = _distance;
            normal = Vector3.Cross(_edge1, _edge2).normalized;
        }

        if (_best == float.MaxValue) return false;

        point = ray.GetPoint(_best);
        if (Vector3.Dot(normal, ray.direction) > 0f) normal = -normal;
        return true;
    }

    private void DrawBrush()
    {
        if (!m_hasHit) return;

        Color _color = Event.current.shift ? new Color(1f, 0.3f, 0.3f) : s_layerColors[m_layer];
        Handles.color = _color;
        Handles.DrawWireDisc(m_hitPoint, m_hitNormal, m_radius, 2f);
        Handles.color = new Color(_color.r, _color.g, _color.b, 0.5f);
        Handles.DrawWireDisc(m_hitPoint, m_hitNormal, m_radius * Mathf.Max(m_hardness, 0.05f));
        Handles.DrawLine(m_hitPoint, m_hitPoint + m_hitNormal * m_radius * 0.5f);
    }

    private void DrawSceneInfo()
    {
        Handles.BeginGUI();
        string _text = $"Pintando: {s_layerNames[m_layer]}   Raio {m_radius:0.##} m{(m_unsaved ? "   (não salvo)" : "")}\n" +
                       "Shift apaga  |  1-4 camada  |  [ ] tamanho  |  Esc sai";
        GUI.Box(new Rect(10, 10, 360, 40), _text, EditorStyles.helpBox);
        Handles.EndGUI();
    }

    // ---------------------------------------------------------------------------------------------
    // Pintura
    // ---------------------------------------------------------------------------------------------

    private void Dab(Vector3 center, bool erase)
    {
        float _radius = m_radius;
        float _radiusSqr = _radius * _radius;
        float _noiseScale = 2.5f / _radius;
        float _softness = Mathf.Max(1f - m_hardness, 0.001f);

        Vector3Int _lo = CellCoord(center - Vector3.one * _radius);
        Vector3Int _hi = CellCoord(center + Vector3.one * _radius);

        for (int z = _lo.z; z <= _hi.z; z++)
        for (int y = _lo.y; y <= _hi.y; y++)
        for (int x = _lo.x; x <= _hi.x; x++)
        {
            int _cell = CellIndex(new Vector3Int(x, y, z));
            for (int k = m_cellStart[_cell]; k < m_cellStart[_cell + 1]; k++)
            {
                int _texel = m_cellTexels[k];
                Vector3 _position = m_texelPositions[_texel];
                float _distanceSqr = (_position - center).sqrMagnitude;
                if (_distanceSqr >= _radiusSqr) continue;

                float _falloff = Mathf.Clamp01((1f - Mathf.Sqrt(_distanceSqr) / _radius) / _softness);
                _falloff = _falloff * _falloff * (3f - 2f * _falloff);

                if (m_irregularity > 0f)
                {
                    float _noise = FractalNoise(_position * _noiseScale + m_noiseOffset);
                    _falloff *= Mathf.Lerp(1f, Mathf.SmoothStep(0f, 1f, _noise * 1.6f - 0.3f), m_irregularity);
                }

                float _amount = _falloff * m_strength * k_dabAmount;
                if (_amount <= 0f) continue;

                int _index = m_texelIndices[_texel];
                PaintTexel(_index, _amount, erase);
                MarkDirty(_index % m_width, _index / m_width);
            }
        }
    }

    // A camada pintada sobe e as que estão por cima dela descem, então ela sempre aparece por cima.
    // Apagando, só a camada selecionada desce.
    private void PaintTexel(int index, float amount, bool erase)
    {
        int _offset = index * 3;
        if (erase)
        {
            if (m_layer > 0) m_channels[_offset + m_layer - 1] *= 1f - amount;
            return;
        }

        for (int _layer = 1; _layer <= 3; _layer++)
        {
            int _channel = _offset + _layer - 1;
            if (_layer == m_layer) m_channels[_channel] += (1f - m_channels[_channel]) * amount;
            else if (_layer > m_layer) m_channels[_channel] *= 1f - amount;
        }
    }

    private void Fill(int layer, string undoName)
    {
        for (int i = 0; i < m_width * m_height; i++)
        {
            for (int _layer = 1; _layer <= 3; _layer++)
            {
                if (_layer == layer) m_channels[i * 3 + _layer - 1] = 1f;
                else if (_layer > layer) m_channels[i * 3 + _layer - 1] = 0f;
            }
        }

        MarkDirty(0, 0);
        MarkDirty(m_width - 1, m_height - 1);
        UploadDirty();
        CommitChange(undoName);
        SceneView.RepaintAll();
    }

    private void MarkDirty(int x, int y)
    {
        if (!m_hasDirty)
        {
            m_dirtyMinX = m_dirtyMaxX = x;
            m_dirtyMinY = m_dirtyMaxY = y;
            m_hasDirty = true;
            return;
        }

        if (x < m_dirtyMinX) m_dirtyMinX = x;
        if (x > m_dirtyMaxX) m_dirtyMaxX = x;
        if (y < m_dirtyMinY) m_dirtyMinY = y;
        if (y > m_dirtyMaxY) m_dirtyMaxY = y;
    }

    private void UploadDirty()
    {
        if (!m_hasDirty) return;

        for (int y = m_dirtyMinY; y <= m_dirtyMaxY; y++)
        {
            for (int x = m_dirtyMinX; x <= m_dirtyMaxX; x++)
            {
                int _index = y * m_width + x;
                int _offset = _index * 3;
                m_pixels[_index] = new Color32(ToByte(m_channels[_offset]), ToByte(m_channels[_offset + 1]), ToByte(m_channels[_offset + 2]), 255);
            }
        }

        m_workingTexture.SetPixelData(m_pixels, 0);
        m_workingTexture.Apply(false);
        m_hasDirty = false;
        m_unsaved = true;
    }

    private static byte ToByte(float value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);

    private void PixelsToChannels()
    {
        for (int i = 0; i < m_pixels.Length; i++)
        {
            m_channels[i * 3] = m_pixels[i].r / 255f;
            m_channels[i * 3 + 1] = m_pixels[i].g / 255f;
            m_channels[i * 3 + 2] = m_pixels[i].b / 255f;
        }
    }

    // Ruído de valor 3D com 2 oitavas, entre 0 e 1
    private static float FractalNoise(Vector3 p) => ValueNoise(p) * 0.65f + ValueNoise(p * 2.13f + new Vector3(17.1f, 3.7f, 9.2f)) * 0.35f;

    private static float ValueNoise(Vector3 p)
    {
        int _x = Mathf.FloorToInt(p.x), _y = Mathf.FloorToInt(p.y), _z = Mathf.FloorToInt(p.z);
        float _fx = p.x - _x, _fy = p.y - _y, _fz = p.z - _z;
        _fx = _fx * _fx * (3f - 2f * _fx);
        _fy = _fy * _fy * (3f - 2f * _fy);
        _fz = _fz * _fz * (3f - 2f * _fz);

        float _x00 = Mathf.Lerp(Hash(_x, _y, _z), Hash(_x + 1, _y, _z), _fx);
        float _x10 = Mathf.Lerp(Hash(_x, _y + 1, _z), Hash(_x + 1, _y + 1, _z), _fx);
        float _x01 = Mathf.Lerp(Hash(_x, _y, _z + 1), Hash(_x + 1, _y, _z + 1), _fx);
        float _x11 = Mathf.Lerp(Hash(_x, _y + 1, _z + 1), Hash(_x + 1, _y + 1, _z + 1), _fx);
        return Mathf.Lerp(Mathf.Lerp(_x00, _x10, _fy), Mathf.Lerp(_x01, _x11, _fy), _fz);
    }

    private static float Hash(int x, int y, int z)
    {
        unchecked
        {
            uint _h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791);
            _h ^= _h >> 13;
            _h *= 0x5bd1e995;
            _h ^= _h >> 15;
            return (_h & 0xFFFFFF) / 16777215f;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Undo
    // ---------------------------------------------------------------------------------------------

    private void CommitChange(string undoName)
    {
        UploadDirty();
        Undo.IncrementCurrentGroup();
        Undo.RecordObject(m_undoState, undoName);
        m_version = ++m_versionCounter;
        m_undoState.version = m_version;
        m_history[m_version] = (Color32[])m_pixels.Clone();
        m_unsaved = true;

        // Descarta as versões mais antigas quando passar do limite de memória
        long _snapshotBytes = (long)m_pixels.Length * 4;
        int _maxSnapshots = Mathf.Max(2, (int)(k_undoMemoryBudget / _snapshotBytes));
        if (m_history.Count > _maxSnapshots)
        {
            List<int> _versions = new(m_history.Keys);
            _versions.Sort();
            for (int i = 0; i < _versions.Count && m_history.Count > _maxSnapshots; i++)
                if (_versions[i] != m_version) m_history.Remove(_versions[i]);
        }

        Repaint();
    }

    private void OnUndoRedo()
    {
        if (!m_painting || m_undoState == null || m_undoState.version == m_version) return;

        if (!m_history.TryGetValue(m_undoState.version, out Color32[] _snapshot))
        {
            Debug.LogWarning("Essa pincelada é antiga demais para desfazer (limite de memória do histórico).");
            m_version = m_undoState.version;
            return;
        }

        m_version = m_undoState.version;
        System.Array.Copy(_snapshot, m_pixels, m_pixels.Length);
        PixelsToChannels();
        m_workingTexture.SetPixelData(m_pixels, 0);
        m_workingTexture.Apply(false);
        m_unsaved = true;
        SceneView.RepaintAll();
        Repaint();
    }

    // ---------------------------------------------------------------------------------------------
    // Preferências
    // ---------------------------------------------------------------------------------------------

    private void LoadPrefs()
    {
        m_layer = EditorPrefs.GetInt(k_prefsPrefix + "Layer", m_layer);
        m_radius = EditorPrefs.GetFloat(k_prefsPrefix + "Radius", m_radius);
        m_strength = EditorPrefs.GetFloat(k_prefsPrefix + "Strength", m_strength);
        m_hardness = EditorPrefs.GetFloat(k_prefsPrefix + "Hardness", m_hardness);
        m_irregularity = EditorPrefs.GetFloat(k_prefsPrefix + "Irregularity", m_irregularity);
        m_maskResolution = EditorPrefs.GetInt(k_prefsPrefix + "Resolution", m_maskResolution);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetInt(k_prefsPrefix + "Layer", m_layer);
        EditorPrefs.SetFloat(k_prefsPrefix + "Radius", m_radius);
        EditorPrefs.SetFloat(k_prefsPrefix + "Strength", m_strength);
        EditorPrefs.SetFloat(k_prefsPrefix + "Hardness", m_hardness);
        EditorPrefs.SetFloat(k_prefsPrefix + "Irregularity", m_irregularity);
        EditorPrefs.SetInt(k_prefsPrefix + "Resolution", m_maskResolution);
    }
}
