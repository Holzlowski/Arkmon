Shader "Arkmon/Courtyard Small Particles"
{
 Properties { _Leaf("Leaf shape",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Leaf;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float fog:TEXCOORD1;};
 V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 Frag(V i):SV_Target
 {
 float2 p=i.uv*2-1;
 float dust=pow(saturate(1-dot(p,p)),2);
 float leaf=1-smoothstep(.75,1,abs(p.x)*2.3+abs(p.y));
 float vein=1-smoothstep(.018,.05,abs(p.x));
 half3 tint=i.color.rgb*lerp(1,.78+vein*.16,_Leaf);
 return half4(MixFog(tint,i.fog),i.color.a*lerp(dust,leaf,_Leaf));
 }
 ENDHLSL
 } }
}
