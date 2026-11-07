Shader "Arkmon/Courtyard Sun Shaft"
{
 Properties { _BaseColor("Warm sunlight", Color)=(1.3,.67,.14,.4) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha One
   ZWrite Off
   ZTest LEqual
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half weight:TEXCOORD2; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.weight=i.color.a;return o; }
   half4 Frag(V i):SV_Target
   {
    float edge=pow(saturate(sin(i.uv.x*3.14159265)),1.8);
    float ends=smoothstep(0,.07,i.uv.y)*(1-smoothstep(.55,1,i.uv.y));
    float filaments=.78+.14*sin(i.uv.x*33+i.uv.y*2)+.08*sin(i.uv.x*71);
    float drift=.94+.06*sin(_Time.y*.21+i.uv.y*4);
    float nearFade=smoothstep(.3,2.4,distance(i.world,_WorldSpaceCameraPos));
    return half4(_BaseColor.rgb,_BaseColor.a*i.weight*edge*ends*filaments*drift*nearFade);
   }
   ENDHLSL
  }
 }
}
