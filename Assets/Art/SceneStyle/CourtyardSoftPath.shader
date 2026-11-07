Shader "Arkmon/Courtyard Soft Path"
{
 Properties
 {
  _BaseMap("Earth",2D)="white" {} _BaseColor("Earth tint",Color)=(.54,.39,.24,1)
  _Region("Polish x range",Vector)=(-30,-5,0,0)
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Offset -1, -1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float edge:TEXCOORD3;float fog:TEXCOORD4;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor,_Region;
   CBUFFER_END
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.edge=i.color.r;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float region=smoothstep(_Region.x,_Region.x+2,i.world.x)*(1-smoothstep(_Region.y-2,_Region.y,i.world.x));
    float irregular=.085*sin(i.world.x*1.7+i.world.z*1.1)+.04*sin(i.world.x*6.3-i.world.z*4.2);
    float alpha=lerp(1,1-smoothstep(.44,1,i.edge+irregular),region);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half3 light=max(SampleSH(normalize(i.normal)),half3(.09,.11,.16));
    light+=sun.color*saturate(dot(normalize(i.normal),sun.direction))*sun.shadowAttenuation;
    return half4(MixFog(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*light,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
