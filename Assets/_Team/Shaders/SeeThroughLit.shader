// Igual ao Standard (Built-in Render Pipeline), mas abre um furo circular em volta do player.
// Os valores do furo vêm do componente SeeThroughWall na câmera.
Shader "TaleOfTheLizards/SeeThroughLit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Sem addshadow: a sombra vem do Fallback e a parede continua fazendo sombra inteira
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        #include "SeeThroughWall.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float4 screenPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            SeeThroughClip(IN.worldPos, IN.screenPos);

            fixed4 _albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = _albedo.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _albedo.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
