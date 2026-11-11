Shader "Arkmon/Starter Summoning Paper"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
  [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
  _Wind("Hair wind",Float)=0
  _WindTime("Hair wind time",Float)=0
  [HideInInspector] _RendererColor("Renderer Color",Color)=(1,1,1,1)
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off ZWrite Off
  Pass {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   #include "../Art/SceneStyle/AurenfeldMorning.hlsl"
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; float3 world:TEXCOORD4; };
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color; float _Wind; float _WindTime;
   CBUFFER_END
   Varyings Vert(Attributes input) {
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    Varyings o=CommonUnlitVertex(input);
    o.world=TransformObjectToWorld(input.positionOS);
    o.color=input.color*_Color*unity_SpriteColor;
    return o;
   }
   half4 Frag(Varyings i):SV_Target {
        float mask=(1-smoothstep(.26,.35,i.uv.x))*smoothstep(.735,.755,i.uv.y)*(1-smoothstep(.845,.865,i.uv.y));
    i.uv.y += sin(_WindTime*8.5+i.uv.x*15)*mask*_Wind*.009;
    i.uv.x += cos(_WindTime*8.5+i.uv.x*15)*mask*_Wind*.004;
    half4 c=CommonUnlitFragment(i,i.color);
    c.rgb=AurenfeldMorningLight(c.rgb,i.world,.3);
    return c;
   }
   ENDHLSL
  }
 }
}
