Shader "Arkmon/Aurenfeld Foothills"
{
 Properties { _HarvestCanopy("Painted foothill surface",2D)="white" {} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "../Art/SceneStyle/AurenfeldMorning.hlsl"
   TEXTURE2D(_HarvestCanopy); SAMPLER(sampler_HarvestCanopy);
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half3 color:COLOR; };
   V Vert(A i)
   {
    V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
    float3 normal=TransformObjectToWorldNormal(i.normalOS);Light sun=GetMainLight();
    half shade=.68+.32*saturate(dot(normal,sun.direction));
    o.color=i.color.rgb*shade;return o;
   }
   half4 Frag(V i):SV_Target
   {
    // Broad, softly varied paint; no fine grain, hard contour stripes or global distance fog.
    float brush=AurenfeldCloudNoise(i.world.xz*.075);
    float2 uv=i.world.xz*.045+float2(sin(i.world.z*.027),sin(i.world.x*.023))*.10;
    half3 a=SAMPLE_TEXTURE2D(_HarvestCanopy,sampler_HarvestCanopy,uv).rgb;
    half3 b=SAMPLE_TEXTURE2D(_HarvestCanopy,sampler_HarvestCanopy,uv*1.37+float2(.37,.61)).rgb;
    half grain=clamp(dot(lerp(a,b,.28),half3(.30,.59,.11))/.49,.62,1.55);
    float patches=AurenfeldCloudNoise(i.world.xz*.036);
    float distantLeft=lerp(.55,1,1-smoothstep(-65,40,i.world.z))*smoothstep(345,410,-i.world.x);
    half3 color=i.color*(.90+.20*brush)*lerp(1,grain,lerp(.70,.23,distantLeft))*(.88+.24*patches);
    color*=lerp(half3(1.045,1.015,.96),half3(.77,.81,.85),AurenfeldCloud(i.world));
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
