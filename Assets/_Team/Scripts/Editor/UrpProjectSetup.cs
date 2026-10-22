using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Passa o projeto do Built-in para o URP: cria o URP Asset + Renderer, liga em todos os níveis de qualidade
// e converte os materiais (Standard -> URP/Lit etc.).
// Não usa Converters.RunInBatchMode porque no URP 17.3 ele quebra com MissingMethodException.
public static class UrpProjectSetup
{
    private const string k_folder = "Assets/Settings";
    private const string k_rendererPath = k_folder + "/URP_Renderer.asset";
    private const string k_assetPath = k_folder + "/URP_Asset.asset";
    private const string k_postProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

    [MenuItem("Tools/Tale of the Lizards/Configurar projeto para URP")]
    private static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Configurar URP",
                "Cria o URP Asset, liga ele em todos os níveis de qualidade e converte os materiais do Built-in para o URP. " +
                "Salve as cenas antes. Continuar?", "Converter", "Cancelar"))
            return;

        UniversalRenderPipelineAsset _asset = GetOrCreatePipelineAsset();

        // Pipeline padrão + todos os níveis de qualidade
        GraphicsSettings.defaultRenderPipeline = _asset;
        int _currentLevel = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = _asset;
        }
        QualitySettings.SetQualityLevel(_currentLevel, false);

        // Converte todos os materiais do projeto que têm um upgrader para URP
        var _upgraders = MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));
        MaterialUpgrader.UpgradeProjectFolder(_upgraders, "Convertendo materiais para URP");

        AssetDatabase.SaveAssets();
        Debug.Log($"[UrpProjectSetup] Projeto configurado para URP ({k_assetPath}).");
    }

    private static UniversalRenderPipelineAsset GetOrCreatePipelineAsset()
    {
        var _existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(k_assetPath);
        if (_existing != null)
            return _existing;

        if (!AssetDatabase.IsValidFolder(k_folder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var _renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(k_rendererPath);
        if (_renderer == null)
        {
            _renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            _renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(k_postProcessDataPath);
            AssetDatabase.CreateAsset(_renderer, k_rendererPath);
        }

        UniversalRenderPipelineAsset _asset = UniversalRenderPipelineAsset.Create(_renderer);
        AssetDatabase.CreateAsset(_asset, k_assetPath);
        AssetDatabase.SaveAssets();
        return _asset;
    }
}
