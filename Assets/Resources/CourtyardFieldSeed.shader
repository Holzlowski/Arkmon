Shader "Arkmon/Field Seed"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float fog:TEXCOORD1; float distance:TEXCOORD2; };
            V Vert(A i)
            {
                V o;float3 world=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(world);o.uv=i.uv;o.color=i.color;
                o.distance=distance(world,_WorldSpaceCameraPos);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float seed=1-smoothstep(.25,1,length(p*float2(2.2,1.3)));
                float tuft=(1-smoothstep(.1,.85,length((p-float2(0,.32))*float2(1.6,2.1))))*.28;
                float visibility=smoothstep(1,3,i.distance)*(1-smoothstep(32,45,i.distance));
                return half4(MixFog(i.color.rgb,i.fog),max(seed,tuft)*i.color.a*visibility);
            }
            ENDHLSL
        }
    }
}
