Shader "Arkmon/Courtyard Painted Ground Patch"
{
 Properties
 {
  _BaseMap("Painted ground atlas",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
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
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float edge:TEXCOORD1;float fog:TEXCOORD2;};
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor,_Tile;float _TextureScale,_FogStrength;
   CBUFFER_END
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.edge=i.color.r;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    float2 uv=i.world.xz*_TextureScale;
    float2 at=(.01+frac(uv)*.98)*_Tile.xy+_Tile.zw;
    half3 paint=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,at,clamp(ddx(uv)*_Tile.xy,-.002,.002),clamp(ddy(uv)*_Tile.xy,-.002,.002)).rgb*_BaseColor.rgb;
    float irregular=.04*sin(i.world.x*8+i.world.z*4);
    float alpha=(1-smoothstep(.62,1,i.edge+irregular))*_BaseColor.a;
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half3 light=max(SampleSH(half3(0,1,0)),half3(.09,.11,.16))+sun.color*saturate(sun.direction.y)*sun.shadowAttenuation;
    return half4(lerp(paint*light,MixFog(paint*light,i.fog),_FogStrength),alpha);
   }
   ENDHLSL
  }
 }
}
