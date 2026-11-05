Shader "Arkmon/Courtyard Painted Horizon"
{
 Properties
 {
  _BaseMap("Painting",2D)="white" {}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _Contrast("Paint contrast",Float)=1.12
  _Haze("Low valley haze",Range(0,1))=.1
  _HazeColor("Valley haze color",Color)=(.62,.69,.72,1)
 }
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
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor,_HazeColor; float _Contrast,_Haze;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V Vert(A i) {V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
   half4 Frag(V i):SV_Target
   {
    half4 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    half3 color=max(0,(paint.rgb-.18)*_Contrast+.18)*_BaseColor.rgb;
    // A distant painting already contains atmospheric perspective. Global distance fog
    // would bleach the entire mountain card; add only a restrained haze at its base.
    color=lerp(color,_HazeColor.rgb,_Haze*(1-smoothstep(.06,.42,i.uv.y)));
    return half4(color,paint.a*_BaseColor.a);
   }
   ENDHLSL
  }
 }
}
