Shader "Arkmon/Courtyard Illustrated Surface"
{
 Properties
 {
  _BaseMap("Painted atlas",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
  _Tile("Atlas scale and offset",Vector)=(.5,.5,0,0)
  _TextureScale("Repeats per metre",Float)=.4
  _ObjectScale("Object metres",Vector)=(1,1,1,0)
  _Wood("Align wood grain",Float)=0 _GrainAxis("Long axis",Float)=1
  _EdgeWidth("Ink edge metres",Float)=0
  _FaceAlignedMasonry("Face-aligned wall projection",Float)=0
  _Saturation("Paint saturation",Range(0,2))=.98
  _Contrast("Paint contrast",Range(.5,1.5))=1.05
  _AmbientFill("Illustrated shadow fill",Range(0,1))=.43
  _SunStrength("Sunlight strength",Range(0,1))=.50
  _ShadowFloor("Sunlight retained in shadow",Range(0,1))=.28
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
   struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;float3 normal:TEXCOORD2;float3 normalOS:TEXCOORD3;float fog:TEXCOORD4; };
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor,_Tile,_ObjectScale;float _TextureScale,_Wood,_GrainAxis,_EdgeWidth;
   float _Saturation,_Contrast,_AmbientFill,_SunStrength,_ShadowFloor,_FaceAlignedMasonry;
   CBUFFER_END
   V Vert(A i)
   {
    V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
    o.local=i.positionOS.xyz;o.normal=TransformObjectToWorldNormal(i.normalOS);o.normalOS=i.normalOS;o.fog=ComputeFogFactor(o.positionCS.z);return o;
   }
   half3 Paint(float2 uv)
   {
    // Keep sampling within the selected quadrant, including the filtered texture footprint.
    float2 at=(.01+frac(uv)*.98)*_Tile.xy+_Tile.zw;
    float2 dx=clamp(ddx(uv)*_Tile.xy,-.002,.002),dy=clamp(ddy(uv)*_Tile.xy,-.002,.002);
    return SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,at,dx,dy).rgb;
   }
   half4 Frag(V i):SV_Target
   {
    float3 p=i.local*_ObjectScale.xyz,an=abs(normalize(i.normalOS));
    float2 uv;float edge;
    float3 edges=(.5-abs(i.local))*_ObjectScale.xyz;
    if(an.x>an.y && an.x>an.z){uv=p.zy;edge=min(edges.z,edges.y);if(_Wood>.5 && _GrainAxis>1.5)uv=uv.yx;}
    else if(an.y>an.z){uv=p.xz;edge=min(edges.x,edges.z);if(_Wood>.5 && _GrainAxis<.5)uv=uv.yx;}
    else {uv=p.xy;edge=min(edges.x,edges.y);if(_Wood>.5 && _GrainAxis<.5)uv=uv.yx;}
    // Project along each vertical face so diagonal tower facets keep the same stone width as straight walls.
    if(_FaceAlignedMasonry>.5 && an.y<.5)
    {
     float3 tangent=normalize(float3(i.normalOS.z,0,-i.normalOS.x));
     uv=float2(dot(p,tangent),p.y);
    }
    half3 paint=Paint(uv*_TextureScale)*_BaseColor.rgb;
    // Preserve the original painted colors, then add modest local contrast in linear light.
    paint=max(0,lerp(dot(paint,half3(.2126,.7152,.0722)).xxx,paint,_Saturation));
    paint=max(0,(paint-.18)*_Contrast+.18);
    if(_EdgeWidth>0)paint*=lerp(.42,1,smoothstep(0,max(_EdgeWidth,fwidth(edge)*1.5),edge));
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half diffuse=saturate(dot(normalize(i.normal),sun.direction));
    half shade=lerp(_ShadowFloor,1,sun.shadowAttenuation);
    half3 light=half3(.95,.98,1.03)*_AmbientFill+sun.color*(_SunStrength*diffuse*shade);
    return half4(MixFog(paint*light,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
