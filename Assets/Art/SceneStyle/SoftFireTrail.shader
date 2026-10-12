Shader "Arkmon/Soft Fire Trail"
{
    Properties { _BaseColor("Color", Color)=(1.6,0.5,0.05,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv; output.color=input.color; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half edge=saturate(1-abs(input.uv.y*2-1));
                half alpha=smoothstep(0,0.6,edge)*input.color.a*_BaseColor.a;
                half3 color=lerp(_BaseColor.rgb,half3(2,1.2,0.3),edge*edge)*input.color.rgb;
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
