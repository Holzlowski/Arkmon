Shader "Arkmon/Courtyard Sun Dust"
{
 Properties { _Visibility("Visibility", Range(0,1))=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+6" "RenderType"="Transparent" }
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
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 world:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
   half _Visibility;
   CBUFFER_END
   V Vert(A i)
   {
    V o; o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.color=i.color;return o;
   }
   half4 Frag(V i):SV_Target
   {
    float radius=length(i.uv*2-1);
    float speck=(1-smoothstep(.08,.5,radius))*.75+(1-smoothstep(.2,1,radius))*.25;
    // Dust lights up near the beam fan, then dims as it drifts away.
    float3 start=float3(-43.45,6.17,6.9), end=float3(-35.44,3.04,8.33);
    float3 axis=end-start;
    float along=saturate(dot(i.world-start,axis)/dot(axis,axis));
    float beam=1-smoothstep(.5,1.7,distance(i.world,start+along*axis));
    float nearFade=smoothstep(.3,1.5,distance(i.world,_WorldSpaceCameraPos));
    return half4(i.color.rgb,i.color.a*speck*beam*nearFade*_Visibility);
   }
   ENDHLSL
  }
 }
}
