Shader "Arkmon/Quest Shimmer"
{
 Properties { _Tint("Quest tint",Color)=(1,.73,.34,1) _Strength("Strength",Range(0,1))=.12 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One ZWrite Off Offset -1,-1
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint; float _Strength;
   CBUFFER_END
   V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);return o; }
   half4 Frag(V i):SV_Target
   {
    float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)))),2);
    float sweep=pow(.5+.5*sin(i.world.y*3-i.world.x*.9-_Time.y*1.1),12);
    float sparkle=pow(.5+.5*sin(i.world.x*39+i.world.y*61+i.world.z*17+_Time.y*.7),32)*sweep;
    float pulse=.85+.15*sin(_Time.y*1.7);
    return half4(_Tint.rgb,_Strength*pulse*(.08+.30*rim+.40*sweep+.45*sparkle));
   }
   ENDHLSL
  }
 }
}
