#ifndef LAYERED_PAINT_INCLUDED
#define LAYERED_PAINT_INCLUDED

// Shader de pintura em camadas para objetos do ProBuilder.
// - 4 camadas de textura projetadas em triplanar (coordenadas de mundo, não estica nas paredes)
// - A máscara pintada (_PaintMask) fica no UV2 do objeto: R = camada 1, G = camada 2, B = camada 3
// - Cada camada pode quebrar a repetição: cada ladrilho ganha deslocamento/espelho/rotação aleatórios

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Assets/_Team/Shaders/SeeThroughWall.hlsl"

TEXTURE2D(_Layer0Map); SAMPLER(sampler_Layer0Map);
TEXTURE2D(_Layer1Map);
TEXTURE2D(_Layer2Map);
TEXTURE2D(_Layer3Map);
TEXTURE2D(_Layer0Normal);
TEXTURE2D(_Layer1Normal);
TEXTURE2D(_Layer2Normal);
TEXTURE2D(_Layer3Normal);
// Setada por renderer (MaterialPropertyBlock) pelo componente LayeredPaintSurface
TEXTURE2D(_PaintMask); SAMPLER(sampler_PaintMask);

CBUFFER_START(UnityPerMaterial)
    half4 _Layer0Color;
    half4 _Layer1Color;
    half4 _Layer2Color;
    half4 _Layer3Color;
    float _Layer0Size;
    float _Layer1Size;
    float _Layer2Size;
    float _Layer3Size;
    float _Layer0NoTile;
    float _Layer1NoTile;
    float _Layer2NoTile;
    float _Layer3NoTile;
    half _Layer0NormalStrength;
    half _Layer1NormalStrength;
    half _Layer2NormalStrength;
    half _Layer3NormalStrength;
    half _Layer0Smoothness;
    half _Layer1Smoothness;
    half _Layer2Smoothness;
    half _Layer3Smoothness;
    half _BlendSharpness;
    half _TriplanarSharpness;
    half _NoTileBlend;
    float _SeeThrough;
CBUFFER_END

struct LPLayer
{
    half3 albedo;
    half3 normalWS;
    half height;
};

// Transformação aleatória de um ladrilho: deslocamento + espelho em x/y + troca de eixos (8 orientações)
struct LPTile
{
    float2 offset;
    float2 flip;
    float swap;
};

float4 LP_Hash4(float2 p)
{
    return frac(sin(float4(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)),
                           dot(p, float2(419.2, 371.9)), dot(p, float2(97.3, 251.1)))) * 43758.5453);
}

LPTile LP_GetTile(float2 cell)
{
    float4 _h = LP_Hash4(cell);
    LPTile _tile;
    _tile.offset = _h.xy;
    _tile.flip = float2(_h.z < 0.5 ? -1.0 : 1.0, _h.w < 0.5 ? -1.0 : 1.0);
    _tile.swap = step(0.5, frac(_h.x * 13.17));
    return _tile;
}

float2 LP_TileVector(LPTile tile, float2 v)
{
    v = tile.swap > 0.5 ? v.yx : v;
    return v * tile.flip;
}

// Desfaz a transformação no xy do normal map, para ele continuar apontando para o lado certo
half2 LP_UntileNormal(LPTile tile, half2 n)
{
    n *= (half2)tile.flip;
    return tile.swap > 0.5 ? n.yx : n;
}

half3 LP_SampleNormalGrad(TEXTURE2D_PARAM(normalMap, samp), float2 uv, float2 dx, float2 dy, half strength)
{
#if defined(_LAYER_NORMALS)
    return UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(normalMap, samp, uv, dx, dy), strength);
#else
    return half3(0, 0, 1);
#endif
}

void LP_AddCorner(TEXTURE2D_PARAM(albedoMap, sampA), TEXTURE2D_PARAM(normalMap, sampN), float2 uv, float2 dx, float2 dy,
                  float2 cell, half weight, half strength, inout half4 albedo, inout half3 normalTS)
{
    if (weight <= 0.001)
        return;

    LPTile _tile = LP_GetTile(cell);
    float2 _uv = LP_TileVector(_tile, uv) + _tile.offset;
    float2 _dx = LP_TileVector(_tile, dx);
    float2 _dy = LP_TileVector(_tile, dy);

    albedo += SAMPLE_TEXTURE2D_GRAD(albedoMap, sampA, _uv, _dx, _dy) * weight;
    half3 _n = LP_SampleNormalGrad(TEXTURE2D_ARGS(normalMap, sampN), _uv, _dx, _dy, strength);
    _n.xy = LP_UntileNormal(_tile, _n.xy);
    normalTS += _n * weight;
}

// Amostra uma textura em um plano 2D. Com noTile ligado, mistura 4 ladrilhos vizinhos com transformações aleatórias
void LP_SamplePlane(TEXTURE2D_PARAM(albedoMap, sampA), TEXTURE2D_PARAM(normalMap, sampN), float2 uv, float noTile,
                    half strength, out half4 albedo, out half3 normalTS)
{
    float2 _dx = ddx(uv);
    float2 _dy = ddy(uv);

    if (noTile < 0.5)
    {
        albedo = SAMPLE_TEXTURE2D_GRAD(albedoMap, sampA, uv, _dx, _dy);
        normalTS = LP_SampleNormalGrad(TEXTURE2D_ARGS(normalMap, sampN), uv, _dx, _dy, strength);
        return;
    }

    float2 _cell = floor(uv);
    float2 _b = smoothstep(0.5 - _NoTileBlend, 0.5 + _NoTileBlend, frac(uv));

    albedo = 0;
    normalTS = 0;
    LP_AddCorner(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), uv, _dx, _dy, _cell + float2(0, 0), (1 - _b.x) * (1 - _b.y), strength, albedo, normalTS);
    LP_AddCorner(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), uv, _dx, _dy, _cell + float2(1, 0), _b.x * (1 - _b.y), strength, albedo, normalTS);
    LP_AddCorner(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), uv, _dx, _dy, _cell + float2(0, 1), (1 - _b.x) * _b.y, strength, albedo, normalTS);
    LP_AddCorner(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), uv, _dx, _dy, _cell + float2(1, 1), _b.x * _b.y, strength, albedo, normalTS);
}

// Triplanar com "whiteout blend" para os normais (Ben Golus)
LPLayer LP_SampleLayer(TEXTURE2D_PARAM(albedoMap, sampA), TEXTURE2D_PARAM(normalMap, sampN), half4 color, float size,
                       float noTile, half strength, float3 positionWS, half3 normalWS, half3 triWeights)
{
    float3 _p = positionWS / max(size, 0.001);
    half3 _axisSign = half3(normalWS.x < 0 ? -1 : 1, normalWS.y < 0 ? -1 : 1, normalWS.z < 0 ? -1 : 1);

    half4 _albedo = 0;
    half3 _normal = 0;
    half4 _a;
    half3 _tn;

    if (triWeights.x > 0.001)
    {
        LP_SamplePlane(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), float2(_p.z * _axisSign.x, _p.y), noTile, strength, _a, _tn);
        _tn.x *= _axisSign.x;
        _albedo += _a * triWeights.x;
        _normal += half3(_tn.xy + normalWS.zy, abs(_tn.z) * normalWS.x).zyx * triWeights.x;
    }
    if (triWeights.y > 0.001)
    {
        LP_SamplePlane(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), float2(_p.x * _axisSign.y, _p.z), noTile, strength, _a, _tn);
        _tn.x *= _axisSign.y;
        _albedo += _a * triWeights.y;
        _normal += half3(_tn.xy + normalWS.xz, abs(_tn.z) * normalWS.y).xzy * triWeights.y;
    }
    if (triWeights.z > 0.001)
    {
        LP_SamplePlane(TEXTURE2D_ARGS(albedoMap, sampA), TEXTURE2D_ARGS(normalMap, sampN), float2(-_p.x * _axisSign.z, _p.y), noTile, strength, _a, _tn);
        _tn.x *= -_axisSign.z;
        _albedo += _a * triWeights.z;
        _normal += half3(_tn.xy + normalWS.xy, abs(_tn.z) * normalWS.z) * triWeights.z;
    }

    LPLayer _layer;
    _layer.height = Luminance(_albedo.rgb);
    _layer.albedo = _albedo.rgb * color.rgb;
    _layer.normalWS = normalize(_normal);
    return _layer;
}

// Usa o brilho da textura como "altura": a camada de cima aparece primeiro nas partes claras (ex: pedras antes da terra)
half LP_HeightBlend(half maskValue, half height)
{
    return saturate((maskValue - (1.0h - height) * _BlendSharpness) / max(1.0h - _BlendSharpness, 0.001h));
}

#define LP_BLEND_LAYER(N, maskValue) \
    if (maskValue > 0.001) \
    { \
        LPLayer _layer##N = LP_SampleLayer(TEXTURE2D_ARGS(_Layer##N##Map, sampler_Layer0Map), TEXTURE2D_ARGS(_Layer##N##Normal, sampler_Layer0Map), \
            _Layer##N##Color, _Layer##N##Size, _Layer##N##NoTile, _Layer##N##NormalStrength, positionWS, normalWS, _triWeights); \
        half _t##N = LP_HeightBlend(maskValue, _layer##N.height); \
        albedo = lerp(albedo, _layer##N.albedo, _t##N); \
        outNormalWS = lerp(outNormalWS, _layer##N.normalWS, _t##N); \
        smoothness = lerp(smoothness, _Layer##N##Smoothness, _t##N); \
    }

void LP_ComputeSurface(float3 positionWS, half3 normalWS, float2 maskUV, out half3 albedo, out half3 outNormalWS, out half smoothness)
{
    half3 _triWeights = pow(abs(normalWS), _TriplanarSharpness);
    _triWeights /= max(dot(_triWeights, 1.0h), 0.0001h);

    half4 _mask = SAMPLE_TEXTURE2D(_PaintMask, sampler_PaintMask, maskUV);

    LPLayer _base = LP_SampleLayer(TEXTURE2D_ARGS(_Layer0Map, sampler_Layer0Map), TEXTURE2D_ARGS(_Layer0Normal, sampler_Layer0Map),
        _Layer0Color, _Layer0Size, _Layer0NoTile, _Layer0NormalStrength, positionWS, normalWS, _triWeights);
    albedo = _base.albedo;
    outNormalWS = _base.normalWS;
    smoothness = _Layer0Smoothness;

    // A ordem importa: a camada 3 fica por cima da 2, que fica por cima da 1
    LP_BLEND_LAYER(1, _mask.r)
    LP_BLEND_LAYER(2, _mask.g)
    LP_BLEND_LAYER(3, _mask.b)

    outNormalWS = normalize(outNormalWS);
}

void LP_SeeThroughClip(float4 positionCS)
{
    if (_SeeThrough > 0.5)
        SeeThroughClip(positionCS);
}

// ---------------------------------------------------------------------------------------------
// Forward
// ---------------------------------------------------------------------------------------------
#if defined(LP_FORWARD_PASS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 staticLightmapUV : TEXCOORD1; // UV2: também é o UV da máscara pintada
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float2 maskUV : TEXCOORD2;
    half fogFactor : TEXCOORD3;
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord : TEXCOORD4;
#endif
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD7;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings LayeredPaintVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs _vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = _vertexInput.positionCS;
    output.positionWS = _vertexInput.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.maskUV = input.staticLightmapUV;

#if !defined(_FOG_FRAGMENT)
    output.fogFactor = ComputeFogFactor(_vertexInput.positionCS.z);
#endif

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(_vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(_vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(_vertexInput);
#endif
    return output;
}

void LayeredPaintFragment(Varyings input, out half4 outColor : SV_Target0)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    LP_SeeThroughClip(input.positionCS);
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif

    half3 _vertexNormal = normalize(input.normalWS);
    half3 _albedo;
    half3 _normalWS;
    half _smoothness;
    LP_ComputeSurface(input.positionWS, _vertexNormal, input.maskUV, _albedo, _normalWS, _smoothness);

    SurfaceData _surface = (SurfaceData)0;
    _surface.albedo = _albedo;
    _surface.alpha = 1;
    _surface.metallic = 0;
    _surface.specular = 0;
    _surface.smoothness = _smoothness;
    _surface.normalTS = half3(0, 0, 1);
    _surface.occlusion = 1;

    InputData _input = (InputData)0;
    _input.positionWS = input.positionWS;
    _input.positionCS = input.positionCS;
    _input.normalWS = _normalWS;
    _input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    _input.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    _input.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    _input.shadowCoord = float4(0, 0, 0, 0);
#endif
    _input.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
    _input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if defined(DYNAMICLIGHTMAP_ON)
    _input.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, _input.normalWS);
    _input.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    _input.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(_input.positionWS), _input.normalWS,
        _input.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, _input.shadowMask);
#else
    _input.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, _input.normalWS);
    _input.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

    half4 _color = UniversalFragmentPBR(_input, _surface);
    _color.rgb = MixFog(_color.rgb, _input.fogCoord);
    _color.a = 1;
    outColor = _color;
}
#endif

// ---------------------------------------------------------------------------------------------
// ShadowCaster / DepthOnly / DepthNormals (só geometria, sem amostrar as camadas)
// ---------------------------------------------------------------------------------------------
#if defined(LP_SHADOW_PASS) || defined(LP_DEPTH_PASS) || defined(LP_DEPTH_NORMALS_PASS)
#if defined(LP_SHADOW_PASS)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    float3 _LightDirection;
    float3 _LightPosition;
#endif
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct GeoAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct GeoVaryings
{
    float4 positionCS : SV_POSITION;
    half3 normalWS : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

GeoVaryings GeoVertex(GeoAttributes input)
{
    GeoVaryings output = (GeoVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 _positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

#if defined(LP_SHADOW_PASS)
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        float3 _lightDirectionWS = normalize(_LightPosition - _positionWS);
    #else
        float3 _lightDirectionWS = _LightDirection;
    #endif
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(_positionWS, output.normalWS, _lightDirectionWS)));
#else
    output.positionCS = TransformWorldToHClip(_positionWS);
#endif
    return output;
}

half4 GeoFragment(GeoVaryings input) : SV_Target0
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if !defined(LP_SHADOW_PASS)
    LP_SeeThroughClip(input.positionCS);
#endif
#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif

#if defined(LP_DEPTH_NORMALS_PASS)
    half3 _normalWS = NormalizeNormalPerPixel(input.normalWS);
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 _octNormalWS = PackNormalOctQuadEncode(_normalWS);
        float2 _remapped = saturate(_octNormalWS * 0.5 + 0.5);
        return half4(PackFloat2To888(_remapped), 0.0);
    #else
        return half4(_normalWS, 0.0);
    #endif
#else
    return input.positionCS.z;
#endif
}
#endif

// ---------------------------------------------------------------------------------------------
// Meta (cor usada no bake de luz)
// ---------------------------------------------------------------------------------------------
#if defined(LP_META_PASS)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct MetaAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
};

struct MetaVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float2 maskUV : TEXCOORD2;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV : TEXCOORD3;
    float4 LightCoord : TEXCOORD4;
#endif
};

MetaVaryings LayeredPaintVertexMeta(MetaAttributes input)
{
    MetaVaryings output = (MetaVaryings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.maskUV = input.uv1;
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    return output;
}

half4 LayeredPaintFragmentMeta(MetaVaryings input) : SV_Target
{
    half3 _albedo;
    half3 _normalWS;
    half _smoothness;
    LP_ComputeSurface(input.positionWS, normalize(input.normalWS), input.maskUV, _albedo, _normalWS, _smoothness);

    MetaInput _meta = (MetaInput)0;
    _meta.Albedo = _albedo;
    _meta.Emission = 0;
#ifdef EDITOR_VISUALIZATION
    _meta.VizUV = input.VizUV;
    _meta.LightCoord = input.LightCoord;
#endif
    return UnityMetaFragment(_meta);
}
#endif

#endif
