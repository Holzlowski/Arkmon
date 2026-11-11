Shader "Arkmon/Courtyard Bucket Contents"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Filled("Filled",Float)=0 }
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
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial) float _Filled; CBUFFER_END
   V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o;}
   half4 Frag(V i):SV_Target
   {
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    if(c.g>c.r*1.12 && c.b>c.r*1.08)
    {half light=dot(c.rgb,half3(.3,.59,.11));c.rgb=lerp(half3(.16,.065,.018)*(.9+light*.1),c.rgb,saturate(_Filled));}
    return c*i.color;
   }
   ENDHLSL
  }
 }
}
