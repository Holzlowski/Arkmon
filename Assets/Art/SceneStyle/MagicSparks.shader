Shader "Arkmon/Magic Sparks"
{
    Properties { _BaseColor("Tint", Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V Vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                half core=pow(saturate(1-length(p)),3);
                half rays=max(pow(saturate(1-abs(p.x)),18)*saturate(1-abs(p.y)),
                    pow(saturate(1-abs(p.y)),18)*saturate(1-abs(p.x)));
                return half4(_BaseColor.rgb*i.color.rgb*2, saturate(core+rays*.75)*_BaseColor.a*i.color.a);
            }
            ENDHLSL
        }
    }
}
