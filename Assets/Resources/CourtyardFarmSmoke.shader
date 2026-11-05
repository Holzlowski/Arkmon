Shader "Arkmon/Courtyard Farm Smoke"
{
    Properties { _Tint("Smoke tint", Color)=(.66,.62,.54,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; half fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            CBUFFER_END
            V Vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color*_Tint;o.fog=ComputeFogFactor(o.positionCS.z);return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float r=length(p);
                float edge=1+.07*sin(atan2(p.y,p.x)*5);
                half alpha=(1-smoothstep(.12,edge,r));
                return half4(MixFog(i.color.rgb,i.fog),i.color.a*alpha*alpha);
            }
            ENDHLSL
        }
    }
}
