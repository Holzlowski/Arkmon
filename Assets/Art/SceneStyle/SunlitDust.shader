Shader "Arkmon/Sunlit Dust"
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
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half4 color:COLOR; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float radius=length(i.uv*2-1);
                half alpha=pow(saturate(1-radius),1.6)*i.color.a;
                float3 offset=i.positionWS-float3(-1.05,2.1,3.1);
                float3 axis=normalize(float3(-2.75,-3.7,-6.5));
                float along=dot(offset,axis);
                float radial=length(offset-axis*along);
                alpha*=1-smoothstep(.45,1.05,radial);
                alpha*=1-smoothstep(1.8,2.6,abs(along));
                return half4(i.color.rgb*1.8,alpha);
            }
            ENDHLSL
        }
    }
}
