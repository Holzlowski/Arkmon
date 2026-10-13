Shader "Arkmon/Tower Painted Light"
{
 Properties
 {
  _BaseMap("Painted texture",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
  _TextureScale("World texture scale",Float)=.35 _DetailStrength("Painted detail",Range(0,1))=1
  _Cull("Cull",Float)=2 _Cutoff("Cutoff",Float)=.5
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1; };
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float _TextureScale,_DetailStrength;
   CBUFFER_END
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.normal=TransformObjectToWorldNormal(i.normalOS);o.positionCS=TransformWorldToHClip(o.world);return o;}
   half3 Contribution(Light light,half3 normal)
   { return light.color*saturate(dot(normal,light.direction))*light.distanceAttenuation*light.shadowAttenuation; }
   half4 Frag(V i):SV_Target
   {
    half3 n=normalize(i.normal);float3 w=pow(abs(n),4);w/=max(dot(w,1),.001);float3 p=i.world*_TextureScale;
    half3 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
    paint=lerp(half3(.72,.69,.61),paint,_DetailStrength)*_BaseColor.rgb;
    half3 lighting=max(SampleSH(n),half3(.09,.11,.16));
    lighting+=Contribution(GetMainLight(TransformWorldToShadowCoord(i.world)),n);
    #if defined(_ADDITIONAL_LIGHTS)
    for(uint j=0;j<GetAdditionalLightsCount();j++)lighting+=Contribution(GetAdditionalLight(j,i.world),n);
    #endif
    return half4(paint*lighting,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
