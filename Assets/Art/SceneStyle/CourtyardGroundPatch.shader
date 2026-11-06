Shader "Arkmon/Courtyard Painted Ground Patch"
{
 Properties
 {
  _BaseMap("Painted ground atlas",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
  _EdgeStart("Edge fade begins",Range(0,1))=.62
  _EdgeBreakup("Irregular edge",Range(0,.2))=.04
  _WheelRuts("Worn cart tracks",Range(0,1))=0
  _FogStrength("Distance haze strength",Range(0,1))=1
  _Tile("Atlas scale and offset",Vector)=(.5,.5,0,0) _TextureScale("World repeats",Float)=.4
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-30"}
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Offset -2,-2
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A {float4 positionOS:POSITION;float4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float edge:TEXCOORD1;float fog:TEXCOORD2;float across:TEXCOORD3;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor,_Tile;float _TextureScale,_FogStrength,_WheelRuts,_EdgeStart,_EdgeBreakup;
   CBUFFER_END
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.edge=i.color.r;o.across=i.color.g*2-1;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float2 uv=i.world.xz*_TextureScale;
    float2 at=(.01+frac(uv)*.98)*_Tile.xy+_Tile.zw;
    half3 paint=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,at,clamp(ddx(uv)*_Tile.xy,-.002,.002),clamp(ddy(uv)*_Tile.xy,-.002,.002)).rgb*_BaseColor.rgb;
    float irregular=_EdgeBreakup*(.65*sin(i.world.x*2.7+i.world.z*1.6)+.35*sin(i.world.x*8+i.world.z*4))+_WheelRuts*.055*sin(i.world.x*.9+i.world.z*.8);
    float track=exp(-pow((abs(i.across)-.51)/.13,2));
    float worn=.92+.08*sin(i.world.x*.7+i.world.z*.6);
    paint*=lerp(1,1.08-.28*track*worn,_WheelRuts);
    float alpha=(1-smoothstep(_EdgeStart,1,i.edge+irregular))*_BaseColor.a;
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half3 light=max(SampleSH(half3(0,1,0)),half3(.09,.11,.16))+sun.color*saturate(sun.direction.y)*sun.shadowAttenuation;
    return half4(lerp(paint*light,MixFog(paint*light,i.fog),_FogStrength),alpha);
   }
   ENDHLSL
  }
 }
}
