Shader "Arkmon/Aurenfeld Cloud Sunrays"
{
 Properties { _Visibility("Scenic visibility",Range(0,1))=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+4" "RenderType"="Transparent" }
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
   #include "../Art/SceneStyle/AurenfeldMorning.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Visibility;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float3 ground:TEXCOORD1; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 ground:TEXCOORD1; half weight:TEXCOORD2; };
   V Vert(A i)
   {
    V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.ground=i.ground;o.weight=i.color.a;return o;
   }
   half4 Frag(V i):SV_Target
   {
    // Same world-space pattern and drift as the moving shadows. The sunny gaps
    // extend upwards along the sun direction; no independently sliding light texture.
    // A short spatial blur softens the cloud aperture, not the scenery behind it.
    float cover=AurenfeldCloud(i.ground)*.36;
    cover+=(AurenfeldCloud(i.ground+float3(0,0,7))+AurenfeldCloud(i.ground-float3(0,0,7)))*.22;
    cover+=(AurenfeldCloud(i.ground+float3(0,0,16))+AurenfeldCloud(i.ground-float3(0,0,16)))*.10;
    float gap=pow(saturate(1-cover/max(.001,_AurenfeldClouds.z)),2);
    float ends=smoothstep(0,.24,i.uv.y)*(1-smoothstep(.64,1,i.uv.y));
    float sides=smoothstep(0,.12,i.uv.x)*(1-smoothstep(.88,1,i.uv.x));
    float softDetail=.87+.13*AurenfeldCloudNoise(i.ground.xz*.035);
    float alpha=.13*_Visibility*_AurenfeldMorning*saturate(_AurenfeldClouds.z)*i.weight*gap*ends*sides*softDetail;
    return half4(1.35,.94,.36,alpha);
   }
   ENDHLSL
  }
 }
}
