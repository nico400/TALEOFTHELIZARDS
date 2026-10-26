// Feixe de luz "volumétrico" fake para um plane/quad (URP, Unlit, Additive).
// Textura do feixe × poeira em movimento (duas camadas para dar profundidade),
// cor HDR, bordas somem pelo fresnel e, opcionalmente, some ao encostar na geometria.
Shader "TaleOfTheLizards/VolumetricBeam"
{
    Properties
    {
        [MainTexture] _BeamTex("Beam Texture", 2D) = "white" {}
        [HDR][MainColor] _Color("Color", Color) = (1, 0.9, 0.7, 1)
        _Intensity("Intensity", Range(0, 10)) = 1

        [Header(Dust)]
        _DustTex("Dust Texture", 2D) = "white" {}
        _DustStrength("Dust Strength", Range(0, 1)) = 0.5
        _DustSpeed("Dust Speed (layer 1 XY, layer 2 ZW)", Vector) = (0.005, 0.03, -0.01, 0.015)
        _Dust2Tiling("Dust Layer 2 Tiling", Float) = 1.7

        [Header(Fade)]
        _FresnelPower("Edge Fade (Fresnel Power)", Range(0, 8)) = 1.5
        [Toggle(_SOFT_FADE)] _SoftFade("Soft Fade on Geometry (needs Depth Texture)", Float) = 0
        _SoftFadeDistance("Soft Fade Distance", Range(0.01, 5)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _SOFT_FADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BeamTex); SAMPLER(sampler_BeamTex);
            TEXTURE2D(_DustTex); SAMPLER(sampler_DustTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BeamTex_ST;
                float4 _DustTex_ST;
                half4 _Color;
                half _Intensity;
                half _DustStrength;
                float4 _DustSpeed;
                float _Dust2Tiling;
                half _FresnelPower;
                float _SoftFadeDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvBeam : TEXCOORD0;
                float4 uvDust : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                half fogFactor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(pos.positionWS);
                output.screenPos = ComputeScreenPos(pos.positionCS);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);

                output.uvBeam = TRANSFORM_TEX(input.uv, _BeamTex);
                // A poeira desce devagar; a segunda camada é menor e anda em outra direção pra parecer que tem volume.
                float2 dustUV = TRANSFORM_TEX(input.uv, _DustTex);
                output.uvDust.xy = dustUV + _Time.y * _DustSpeed.xy;
                output.uvDust.zw = dustUV * _Dust2Tiling + float2(0.37, 0.71) + _Time.y * _DustSpeed.zw;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 beam = SAMPLE_TEXTURE2D(_BeamTex, sampler_BeamTex, input.uvBeam).rgb;

                half dust1 = SAMPLE_TEXTURE2D(_DustTex, sampler_DustTex, input.uvDust.xy).r;
                half dust2 = SAMPLE_TEXTURE2D(_DustTex, sampler_DustTex, input.uvDust.zw).r;
                half dust = saturate(dust1 * dust2 * 2.0);
                // Lerp a partir de 1 pra poeira só modular o feixe, sem apagar.
                half dustMask = lerp(1.0, dust, _DustStrength);

                // Plane é double-sided, então usa abs pra funcionar dos dois lados.
                half NdotV = abs(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half edgeFade = pow(NdotV, _FresnelPower);

                half fade = edgeFade;
                #if defined(_SOFT_FADE)
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                    float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                    float fragDepth = input.screenPos.w;
                    fade *= saturate((sceneDepth - fragDepth) / _SoftFadeDistance);
                #endif

                half3 color = beam * dustMask * _Color.rgb * _Color.a * _Intensity * fade;
                // Additive: fog puxa pra preto em vez da cor do fog, senão o feixe acende no fundo.
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
