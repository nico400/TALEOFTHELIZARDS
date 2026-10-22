#ifndef SEE_THROUGH_WALL_INCLUDED
#define SEE_THROUGH_WALL_INCLUDED

#include "UnityCG.cginc"

// Globais setadas pelo SeeThroughWall.cs antes de cada câmera renderizar
float4 _SeeThroughTarget; // xyz = posição do player, w = abertura do furo (0 a 1)
float4 _SeeThroughParams; // x = raio (metros), y = borda suave (metros), z = folga de profundidade (metros)

// Ruído por pixel para a borda com dithering
float SeeThroughNoise(float2 pixel)
{
    const float3 _magic = float3(0.06711056, 0.00583715, 52.9829189);
    return frac(_magic.z * frac(dot(pixel, _magic.xy)));
}

// Descarta o pixel se ele está dentro do círculo em volta do player e na frente dele
void SeeThroughClip(float3 positionWS, float4 screenPos)
{
    float _amount = _SeeThroughTarget.w;
    if (_amount <= 0.0)
        return;

    // Em view space a câmera olha para -z
    float3 _fragVS = mul(UNITY_MATRIX_V, float4(positionWS, 1.0)).xyz;
    float3 _targetVS = mul(UNITY_MATRIX_V, float4(_SeeThroughTarget.xyz, 1.0)).xyz;
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
    float2 _pixel = screenPos.xy / max(screenPos.w, 1e-5) * _ScreenParams.xy;
    float _noise = SeeThroughNoise(_pixel) * 0.98 + 0.01;
    clip(_visible - _noise);
}

#endif
