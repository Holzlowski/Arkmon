Shader "Arkmon/Courtyard Plant Wind"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white" {}
  [HideInInspector] _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor("Renderer Color",Color)=(1,1,1,1)
  [HideInInspector] _PlantShape("Bottom, height, maximum sway",Vector)=(0,1,.04,0)
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off
  ZWrite Off
  Pass
  {
   HLSLPROGRAM
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   struct Attributes { COMMON_2D_INPUTS half4 color:COLOR; };
   struct Varyings { COMMON_2D_OUTPUTS half4 color:COLOR; float3 world:TEXCOORD4; };
   #include "../Art/SceneStyle/AurenfeldMorning.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float4 _PlantShape;
   CBUFFER_END
   Varyings Vert(Attributes input)
   {
    UNITY_SETUP_INSTANCE_ID(input);
    SetUpSpriteInstanceProperties();
    float height=saturate((input.positionOS.y-_PlantShape.x)/max(.001,_PlantShape.y));
    input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
    float3 origin=TransformObjectToWorld(float3(0,0,0));
    float3 right=mul((float3x3)unity_ObjectToWorld,float3(1,0,0));
    float scale=max(.001,length(right));
    float worldHeight=length(mul((float3x3)unity_ObjectToWorld,float3(0,_PlantShape.y,0)));
    float phase=origin.x*.17+origin.z*.11;
    float gust=.65+.35*sin(_Time.y*.27+phase*.38);
    float wave=sin(_Time.y*1.35+phase)+.18*sin(_Time.y*2.15+phase*1.7);
    float direction=dot(right/scale,normalize(float3(.8,0,.6)));
    float bend=min(_PlantShape.z,worldHeight*.075)*gust*wave*direction;
    // The planted edge never moves. Extra geometry bends stems instead of rotating the whole card.
    input.positionOS.x+=height*height*bend/scale;
    Varyings output=CommonUnlitVertex(input);
    output.world=TransformObjectToWorld(input.positionOS);
    output.color=input.color*_Color*unity_SpriteColor;
    return output;
   }
   half4 Frag(Varyings input):SV_Target { half4 c=CommonUnlitFragment(input,input.color); c.rgb=AurenfeldMorningLight(c.rgb,input.world,.2); return c; }
   ENDHLSL
  }
 }
}
