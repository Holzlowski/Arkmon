Shader "Arkmon/Aurenfeld Scenic Paper"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
  [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
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
   half4 _Color;
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
    half4 c=CommonUnlitFragment(i,i.color);
    c.rgb=AurenfeldMorningLight(c.rgb,i.world,.3);
    return c;
   }
   ENDHLSL
  }
 }
}
