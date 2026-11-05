Shader "Arkmon/Courtyard Stream"
{
 Properties { _Deep("Deep water",Color)=(.07,.28,.30,1) _Shallow("Shallows",Color)=(.25,.49,.44,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Cull Off
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2;};
   CBUFFER_START(UnityPerMaterial)
   half4 _Deep,_Shallow;
   CBUFFER_END
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float edge=abs(i.uv.x*2-1);
    float flow=i.uv.y+_Time.y*.45;
    float wave=sin(flow*1.8+sin(i.world.x*2.4+flow*.31)*1.7);
    float glint=smoothstep(.88,.99,wave)*smoothstep(.25,.8,sin(i.world.x*4.3+flow*.57))*.06;
    half3 color=lerp(_Deep.rgb,_Shallow.rgb,smoothstep(.4,1,edge)) + half3(.53,.64,.50)*glint;
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    color*=.75+sun.color*.28*sun.shadowAttenuation;
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
 }
}
