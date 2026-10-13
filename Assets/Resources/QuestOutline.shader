Shader "Arkmon/Quest Door Outline"
{
 Properties { _Tint("Outline",Color)=(1,.73,.34,1) _Strength("Strength",Range(0,1))=.12 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One ZWrite Off Cull Off
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; };
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint; float _Strength;
   CBUFFER_END
   V Vert(A i) { V o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o; }
   half4 Frag(V i):SV_Target
   { return half4(_Tint.rgb*2.6,_Tint.a*_Strength*(.94+.06*sin(_Time.y*1.4))); }
   ENDHLSL
  }
 }
}
