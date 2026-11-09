#ifndef SEE_THROUGH_WALL_INCLUDED
#define SEE_THROUGH_WALL_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

// Globais setadas pelo SeeThroughWall.cs antes de cada câmera renderizar
float4 _SeeThroughTarget; // xyz = posição do player, w = abertura do furo (0 a 1)
float4 _SeeThroughParams; // x = raio (metros), y = borda suave (metros), z = folga de profundidade (metros)

// Descarta o pixel se ele está dentro do círculo em volta do player e na frente dele
void SeeThroughClip(float4 positionCS)
{
    float _amount = _SeeThroughTarget.w;
    if (_amount <= 0.0)
        return;

    float2 _screenUV = GetNormalizedScreenSpaceUV(positionCS);
    float3 _fragWS = ComputeWorldSpacePosition(_screenUV, positionCS.z, UNITY_MATRIX_I_VP);

    // Em view space a câmera olha para -z
    float3 _fragVS = TransformWorldToView(_fragWS);
    float3 _targetVS = TransformWorldToView(_SeeThroughTarget.xyz);
    float _fragDepth = -_fragVS.z;
    float _targetDepth = -_targetVS.z;

    // Só corta o que está entre a câmera e o player (chão atrás/embaixo dele continua inteiro)
    if (_fragDepth <= 0.0 || _targetDepth <= 0.0 || _fragDepth >= _targetDepth - _SeeThroughParams.z)
        return;

    // Distância na tela, convertida para metros na profundidade do player: o furo tem tamanho fixo no mundo
    float2 _offset = _fragVS.xy / _fragDepth - _targetVS.xy / _targetDepth;
    float _dist = length(_offset) * _targetDepth;

    float _radius = _SeeThroughParams.x * _amount;
    float _soft = max(_SeeThroughParams.y, 1e-4);
    float _visible = smoothstep(_radius - _soft, _radius, _dist);

    // Borda suave com dithering (continua opaco, sem problemas de ordenação de transparência)
    float _noise = InterleavedGradientNoise(positionCS.xy, 0) * 0.98 + 0.01;
    clip(_visible - _noise);
}

#if defined(SEE_THROUGH_LITFORWARDPASS)
void LitPassFragmentSeeThrough(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    SeeThroughClip(input.positionCS);
    LitPassFragment(input, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#endif

#if defined(SEE_THROUGH_LITGBUFFERPASS)
GBufferFragOutput LitGBufferPassFragmentSeeThrough(Varyings input)
{
    SeeThroughClip(input.positionCS);
    return LitGBufferPassFragment(input);
}
#endif

#if defined(SEE_THROUGH_DEPTHONLYPASS)
half DepthOnlyFragmentSeeThrough(Varyings input) : SV_TARGET
{
    SeeThroughClip(input.positionCS);
    return DepthOnlyFragment(input);
}
#endif

#if defined(SEE_THROUGH_LITDEPTHNORMALSPASS)
void DepthNormalsFragmentSeeThrough(
    Varyings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    SeeThroughClip(input.positionCS);
    DepthNormalsFragment(input, outNormalWS
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
}
#endif

#endif
