Shader "Arkmon/Aurenfeld Scenic Paper"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
  [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _ZWrite("Depth Write",Float)=0
  [HideInInspector] _AlphaCutoff("Alpha Cutoff",Range(0,1))=0
  [HideInInspector] _OakBranchForeground("Oak Branch Foreground",Float)=0
  [HideInInspector] _OakBranchDip("Oak Branch Dip",Float)=0
  [HideInInspector] _OakPerchShape("Oak Perch Shape",Float)=0
  [HideInInspector] _ContactShadow("Contact Shadow",Vector)=(0,0,0,0)
  [HideInInspector] _RendererColor("Renderer Color",Color)=(1,1,1,1)
 }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off ZWrite [_ZWrite]
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
   half _AlphaCutoff;
   float4 _ContactShadow;
   float _OakPerchShape;
   float _OakBranchDip;
   float _OakBranchForeground;
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
    // Straighten the shallow dip under Rauk using the existing bark and outline.
    float2 fromPerch=(i.uv-float2(449.0/1536.0,319.0/1024.0))*float2(1536,1024);
    float lift=max(0,6-.008*fromPerch.x*fromPerch.x);
    float edgeWeight=1-smoothstep(4,23,abs(fromPerch.y));
    float bend=(1-smoothstep(32,155,abs(fromPerch.x)))*(1-smoothstep(14,90,abs(fromPerch.y)));
    i.uv.y+=_OakBranchDip*bend-_OakPerchShape*lift*edgeWeight/1024;
    half4 c=CommonUnlitFragment(i,i.color);
    clip(c.a-_AlphaCutoff);
    if (_OakBranchForeground > .5)
    {
     // The branch curves upward here, in front of Rauk's trailing feathers.
     float2 texel=float2(i.uv.x,1-i.uv.y)*float2(1536,1024);
     float barkEdge=674+max(0,texel.x-375)*.60;
     clip(float4(texel.x-305,427-texel.x,texel.y-barkEdge,740-texel.y));
     clip(c.a-.4);
     c.a=1;
    }
    c.rgb=AurenfeldMorningLight(c.rgb,i.world,.3);
    float2 contact=(i.world.xy-_ContactShadow.xy)/float2(.25,.055);
    c.rgb*=1-.35*_ContactShadow.w*(1-smoothstep(.15,1,dot(contact,contact)));
    return c;
   }
   ENDHLSL
  }
 }
}
