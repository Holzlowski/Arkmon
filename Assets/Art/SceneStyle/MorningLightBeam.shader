Shader "Arkmon/Morning Light Beam"
{
 Properties { _BaseColor("Morning tint",Color)=(1,.88,.62,.045) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One Cull Off ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   V Vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float edge=pow(saturate(sin(i.uv.x*3.14159)),1.5);
    float lengthFade=smoothstep(0,.1,i.uv.y)*(1-smoothstep(.6,1,i.uv.y));
    float rays=.65+.2*sin(i.uv.x*31+sin(i.uv.y*3))+.15*sin(i.uv.x*67);
    float breath=.95+.05*sin(_Time.y*.35+i.uv.y*4);
    return half4(_BaseColor.rgb,_BaseColor.a*edge*lengthFade*rays*breath);
   }
   ENDHLSL
  }
 }
}
