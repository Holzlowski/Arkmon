Shader "Arkmon/Meadow Wind"
{
    Properties { _WindStrength("Wind strength", Float) = 0.16 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half fog : TEXCOORD0;
            };
            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 position = TransformObjectToWorld(input.positionOS.xyz);
                float wave = sin(position.x * .13 + position.z * .17 + _Time.y * 1.4)
                    + sin(position.z * .3 - _Time.y * .8) * .35;
                float bend = wave * _WindStrength * input.uv.y * input.uv.y;
                position.x += bend;
                position.z += bend * .3;
                output.positionCS = TransformWorldToHClip(position);
                output.color = input.color;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(MixFog(input.color.rgb, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
