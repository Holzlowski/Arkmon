Shader "Arkmon/Meadow Root Contact"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-19" }
        Pass
        {
            Blend DstColor Zero
            ZWrite Off
            Cull Off
            Offset -1,-1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1; };
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(i.positionCS);
                #endif
                float radius=length((i.uv-.5)*2);
                float soft=1-smoothstep(.12,1,radius);
                float fade=1-smoothstep(32,48,distance(i.world,_WorldSpaceCameraPos));
                // Multiplicative occlusion retains the actual ground color, with no black discs.
                half shade=1-.38*soft*soft*fade;
                return half4(shade,shade,shade,1);
            }
            ENDHLSL
        }
    }
}
