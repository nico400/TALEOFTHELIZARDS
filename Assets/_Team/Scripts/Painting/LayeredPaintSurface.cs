using UnityEngine;

// Guarda a máscara pintada com a ferramenta "Pintura de Texturas" (Tools > Tale of the Lizards) e entrega
// ela para o shader TaleOfTheLizards/LayeredPaint. A máscara vai por MaterialPropertyBlock, então vários
// objetos podem dividir o mesmo material (mesmas camadas) e cada um ter a sua pintura.
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class LayeredPaintSurface : MonoBehaviour
{
    private static readonly int s_paintMaskId = Shader.PropertyToID("_PaintMask");

    [Tooltip("Máscara no UV2 do objeto: R = camada 1, G = camada 2, B = camada 3. Criada pela ferramenta de pintura")]
    [SerializeField] private Texture2D m_paintMask;

    private MaterialPropertyBlock m_block;

    public Texture2D PaintMask
    {
        get => m_paintMask;
        set
        {
            m_paintMask = value;
            Apply();
        }
    }

    private void OnEnable() => Apply();

    private void OnValidate() => Apply();

    // previewMask: o editor passa a textura que está sendo pintada (ainda não salva) para ver ao vivo
    public void Apply(Texture previewMask = null)
    {
        MeshRenderer _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null) return;

        m_block ??= new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(m_block);

        Texture _mask = previewMask != null ? previewMask : m_paintMask;
        m_block.SetTexture(s_paintMaskId, _mask != null ? _mask : Texture2D.blackTexture);
        _renderer.SetPropertyBlock(m_block);
    }
}
