Shader "Arkmon/Quest Mote"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 p=abs(i.uv*2-1);
                float halo=pow(saturate(1-length(p)),2);
                float star=pow(saturate(1-p.x-p.y),5);
                return half4(i.color.rgb*3,i.color.a*saturate(halo+star));
            }
            ENDHLSL
        }
    }
}
