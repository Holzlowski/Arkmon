Shader "Arkmon/Courtyard Sun Shaft"
{
 Properties { _BaseColor("Warm sunlight", Color)=(1.3,.67,.14,.4) _BeamOrigin("Beam source",Vector)=(0,0,0,0) }
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
   #include "CourtyardSunMotion.hlsl"
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half weight:TEXCOORD2; };
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   float4 _BeamOrigin;
   CBUFFER_END
   V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz);o.world=_BeamOrigin.xyz+(o.world-_BeamOrigin.xyz)*BreachRayLength(_BeamOrigin.xyz,_Time.y);o.positionCS=TransformWorldToHClip(o.world);o.uv=i.uv;o.weight=i.color.a;return o; }
   half4 Frag(V i):SV_Target
   {
    float edge=pow(saturate(sin(i.uv.x*3.14159265)),1.8);
    float ends=smoothstep(0,.07,i.uv.y)*(1-smoothstep(.55,1,i.uv.y));
    float phase=i.world.z*.73+i.world.x*.11;
    float width=.80+.12*sin(_Time.y*.33+phase);
    float centre=.5+.035*sin(_Time.y*.27+phase);
    edge=pow(saturate(1-abs(i.uv.x-centre)*2/width),1.35);
    float filaments=.76+.15*sin(i.uv.x*33+i.uv.y*2+_Time.y*.18)+.09*sin(i.uv.x*71-_Time.y*.24);
    float cloud=.5+.5*sin(_Time.y*.22+phase+.45*sin(_Time.y*.13+phase*2));
    float drift=.48+.52*smoothstep(.1,.9,cloud);
    drift*=.72+.28*smoothstep(-.55,.7,sin(i.uv.y*8-_Time.y*.38+phase));
    float nearFade=smoothstep(.3,2.4,distance(i.world,_WorldSpaceCameraPos));
    return half4(_BaseColor.rgb,_BaseColor.a*i.weight*edge*ends*filaments*drift*nearFade);
   }
   ENDHLSL
  }
 }
}
