Shader "Arkmon/Tower Contact Shadow"
{
 Properties { _BaseColor("Shadow",Color)=(.055,.04,.035,.22) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
   struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;return o;}
   half4 Frag(V i):SV_Target {float2 p=(i.uv-.5)*2;return half4(_BaseColor.rgb,_BaseColor.a*pow(saturate(1-dot(p,p)),1.5));}
   ENDHLSL
  }
 }
}
