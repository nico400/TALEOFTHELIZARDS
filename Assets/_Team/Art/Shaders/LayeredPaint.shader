// Pintura em camadas para objetos do ProBuilder (Tools > Tale of the Lizards > Pintura de Texturas).
// A lógica fica em LayeredPaint.hlsl; a máscara pintada vem do componente LayeredPaintSurface.
Shader "TaleOfTheLizards/LayeredPaint"
{
    Properties
    {
        [Header(Camada 0 . base)]
        [NoScaleOffset] _Layer0Map("Textura", 2D) = "white" {}
        [NoScaleOffset][Normal] _Layer0Normal("Normal Map", 2D) = "bump" {}
        _Layer0Color("Cor", Color) = (1,1,1,1)
        _Layer0Size("Tamanho do ladrilho (metros)", Float) = 2
        [ToggleUI] _Layer0NoTile("Quebrar repeticao", Float) = 1
        _Layer0NormalStrength("Forca do normal", Range(0, 2)) = 1
        _Layer0Smoothness("Smoothness", Range(0, 1)) = 0.1

        [Header(Camada 1 . pincel R)]
        [NoScaleOffset] _Layer1Map("Textura", 2D) = "white" {}
        [NoScaleOffset][Normal] _Layer1Normal("Normal Map", 2D) = "bump" {}
        _Layer1Color("Cor", Color) = (1,1,1,1)
        _Layer1Size("Tamanho do ladrilho (metros)", Float) = 2
        [ToggleUI] _Layer1NoTile("Quebrar repeticao", Float) = 1
        _Layer1NormalStrength("Forca do normal", Range(0, 2)) = 1
        _Layer1Smoothness("Smoothness", Range(0, 1)) = 0.1

        [Header(Camada 2 . pincel G)]
        [NoScaleOffset] _Layer2Map("Textura", 2D) = "white" {}
        [NoScaleOffset][Normal] _Layer2Normal("Normal Map", 2D) = "bump" {}
        _Layer2Color("Cor", Color) = (1,1,1,1)
        _Layer2Size("Tamanho do ladrilho (metros)", Float) = 2
        [ToggleUI] _Layer2NoTile("Quebrar repeticao", Float) = 1
        _Layer2NormalStrength("Forca do normal", Range(0, 2)) = 1
        _Layer2Smoothness("Smoothness", Range(0, 1)) = 0.1

        [Header(Camada 3 . pincel B)]
        [NoScaleOffset] _Layer3Map("Textura", 2D) = "white" {}
        [NoScaleOffset][Normal] _Layer3Normal("Normal Map", 2D) = "bump" {}
        _Layer3Color("Cor", Color) = (1,1,1,1)
        _Layer3Size("Tamanho do ladrilho (metros)", Float) = 2
        [ToggleUI] _Layer3NoTile("Quebrar repeticao", Float) = 1
        _Layer3NormalStrength("Forca do normal", Range(0, 2)) = 1
        _Layer3Smoothness("Smoothness", Range(0, 1)) = 0.1

        [Header(Mistura)]
        [Toggle(_LAYER_NORMALS)] _UseNormals("Usar normal maps", Float) = 1
        _BlendSharpness("Borda da pintura (0 = suave, 1 = segue o desenho da textura)", Range(0, 0.95)) = 0.5
        _NoTileBlend("Largura da mistura entre ladrilhos", Range(0.05, 0.5)) = 0.2
        _TriplanarSharpness("Nitidez nas quinas (triplanar)", Range(1, 16)) = 8

        [Header(Outros)]
        [ToggleUI] _SeeThrough("Furo do SeeThroughWall (paredes)", Float) = 0
        [HideInInspector][NoScaleOffset] _PaintMask("Mascara pintada", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LayeredPaintVertex
            #pragma fragment LayeredPaintFragment

            #pragma shader_feature_local_fragment _LAYER_NORMALS

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #pragma multi_compile_instancing

            #define LP_FORWARD_PASS
            #include "Assets/_Team/Shaders/LayeredPaint.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GeoVertex
            #pragma fragment GeoFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #define LP_SHADOW_PASS
            #include "Assets/_Team/Shaders/LayeredPaint.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GeoVertex
            #pragma fragment GeoFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #define LP_DEPTH_PASS
            #include "Assets/_Team/Shaders/LayeredPaint.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GeoVertex
            #pragma fragment GeoFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #define LP_DEPTH_NORMALS_PASS
            #include "Assets/_Team/Shaders/LayeredPaint.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LayeredPaintVertexMeta
            #pragma fragment LayeredPaintFragmentMeta
            #pragma shader_feature EDITOR_VISUALIZATION

            #define LP_META_PASS
            #include "Assets/_Team/Shaders/LayeredPaint.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
