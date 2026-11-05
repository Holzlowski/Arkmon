Shader "Arkmon/Farm Ground Contact"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-18" "RenderType"="Transparent" }
  Pass
  {
   Blend DstColor Zero
   ZWrite Off
   Cull Off
   Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float edge:TEXCOORD0;};
   V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.edge=i.color.r;return o;}
   half4 Frag(V i):SV_Target {half shade=1-.28*pow(1-smoothstep(0,1,i.edge),2);return half4(shade,shade,shade,1);}
   ENDHLSL
  }
 }
}
