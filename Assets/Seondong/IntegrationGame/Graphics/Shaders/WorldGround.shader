Shader "Seondong/World Ground"
{
    Properties
    {
        _BaseMap ("Existing rock texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.65,0.68,0.56,1)
        _Detail ("Detail contrast", Range(0,1)) = 0.6
        _TileSize ("World units per repeat", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Detail;
                float _TileSize;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 worldXY : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS; o.worldXY = p.positionWS.xy;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 rock = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.worldXY / _TileSize).rgb;
                return half4(lerp(half3(0.13,0.16,0.15), rock * _Tint.rgb, _Detail), 1);
            }
            ENDHLSL
        }
    }
}
