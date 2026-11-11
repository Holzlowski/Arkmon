Shader "Arkmon/Painted Arm"
{
 Properties { _BaseMap("Paint",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half alpha:TEXCOORD1;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
   V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.alpha=a.color.a;return o;}
   half4 Frag(V i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;c.a*=i.alpha;return c;}
   ENDHLSL
  }
 }
}
