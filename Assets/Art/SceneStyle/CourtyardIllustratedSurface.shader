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
  _WorldMasonry("World-scale masonry projection",Float)=0
  _FaceAlignedMasonry("Face-aligned wall projection",Float)=0
  _FootMoss("Moss: ground height, reach, strength, variation",Vector)=(0,1,0,0)
  _MorningAccent("Local sunlight: center and radius",Vector)=(0,0,0,0)
  _ViewCutaway("Camera cutaway: world center and radius",Vector)=(0,0,0,0)
  _MorningAccentStrength("Local sunlight emphasis",Range(0,1))=0
  _Saturation("Paint saturation",Range(0,2))=.98
  _Contrast("Paint contrast",Range(.5,1.5))=1.05
  _AmbientFill("Illustrated shadow fill",Range(0,1))=.52
  _SunStrength("Sunlight strength",Range(0,1))=.64
  _ShadowFloor("Sunlight retained in shadow",Range(0,1))=.15
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }

  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  // Per-piece paint, scale and light remain distinct inside an instanced draw.
  UNITY_INSTANCING_BUFFER_START(PaintProps)
   UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
   UNITY_DEFINE_INSTANCED_PROP(float4, _Tile)
   UNITY_DEFINE_INSTANCED_PROP(float4, _ObjectScale)
   UNITY_DEFINE_INSTANCED_PROP(float4, _FootMoss)
   UNITY_DEFINE_INSTANCED_PROP(float4, _MorningAccent)
   UNITY_DEFINE_INSTANCED_PROP(float4, _ViewCutaway)
   UNITY_DEFINE_INSTANCED_PROP(float, _TextureScale)
   UNITY_DEFINE_INSTANCED_PROP(float, _Wood)
   UNITY_DEFINE_INSTANCED_PROP(float, _GrainAxis)
   UNITY_DEFINE_INSTANCED_PROP(float, _EdgeWidth)
   UNITY_DEFINE_INSTANCED_PROP(float, _Saturation)
   UNITY_DEFINE_INSTANCED_PROP(float, _Contrast)
   UNITY_DEFINE_INSTANCED_PROP(float, _AmbientFill)
   UNITY_DEFINE_INSTANCED_PROP(float, _SunStrength)
   UNITY_DEFINE_INSTANCED_PROP(float, _ShadowFloor)
   UNITY_DEFINE_INSTANCED_PROP(float, _FaceAlignedMasonry)
   UNITY_DEFINE_INSTANCED_PROP(float, _MorningAccentStrength)
   UNITY_DEFINE_INSTANCED_PROP(float, _WorldMasonry)
  UNITY_INSTANCING_BUFFER_END(PaintProps)
  #define _BaseColor UNITY_ACCESS_INSTANCED_PROP(PaintProps, _BaseColor)
  #define _Tile UNITY_ACCESS_INSTANCED_PROP(PaintProps, _Tile)
  #define _ObjectScale UNITY_ACCESS_INSTANCED_PROP(PaintProps, _ObjectScale)
  #define _FootMoss UNITY_ACCESS_INSTANCED_PROP(PaintProps, _FootMoss)
  #define _MorningAccent UNITY_ACCESS_INSTANCED_PROP(PaintProps, _MorningAccent)
  #define _ViewCutaway UNITY_ACCESS_INSTANCED_PROP(PaintProps, _ViewCutaway)
  #define _TextureScale UNITY_ACCESS_INSTANCED_PROP(PaintProps, _TextureScale)
  #define _Wood UNITY_ACCESS_INSTANCED_PROP(PaintProps, _Wood)
  #define _GrainAxis UNITY_ACCESS_INSTANCED_PROP(PaintProps, _GrainAxis)
  #define _EdgeWidth UNITY_ACCESS_INSTANCED_PROP(PaintProps, _EdgeWidth)
  #define _Saturation UNITY_ACCESS_INSTANCED_PROP(PaintProps, _Saturation)
  #define _Contrast UNITY_ACCESS_INSTANCED_PROP(PaintProps, _Contrast)
  #define _AmbientFill UNITY_ACCESS_INSTANCED_PROP(PaintProps, _AmbientFill)
  #define _SunStrength UNITY_ACCESS_INSTANCED_PROP(PaintProps, _SunStrength)
  #define _ShadowFloor UNITY_ACCESS_INSTANCED_PROP(PaintProps, _ShadowFloor)
  #define _FaceAlignedMasonry UNITY_ACCESS_INSTANCED_PROP(PaintProps, _FaceAlignedMasonry)
  #define _MorningAccentStrength UNITY_ACCESS_INSTANCED_PROP(PaintProps, _MorningAccentStrength)
  #define _WorldMasonry UNITY_ACCESS_INSTANCED_PROP(PaintProps, _WorldMasonry)
  void ClipViewCutaway(float3 world)
  {
   if(_ViewCutaway.w<=0)return;
   float3 ray=world-_WorldSpaceCameraPos;
   float3 target=_ViewCutaway.xyz-_WorldSpaceCameraPos;
   float depth=dot(target,ray)/max(.001,dot(ray,ray));
   // Only remove foreground masonry; walls beyond Elian remain solid.
   if(depth>1.01)clip(length(ray*depth-target)-_ViewCutaway.w);
  }
  ENDHLSL
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma multi_compile_instancing
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
   struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;float3 normal:TEXCOORD2;float3 normalOS:TEXCOORD3;float fog:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);

   V Vert(A i)
   {
    V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
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
    UNITY_SETUP_INSTANCE_ID(i);
    ClipViewCutaway(i.world);
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
    // Opt-in for the ruin: every face samples the same metre-sized stone courses,
    // independent of block scale, mesh authoring units and small broken-face rotations.
    if(_WorldMasonry>.5)
    {
     float3 wn=abs(normalize(i.normal));
     uv=wn.x>wn.y && wn.x>wn.z?i.world.zy:wn.y>wn.z?i.world.xz:i.world.xy;
    }
    half3 paint=Paint(uv*_TextureScale)*_BaseColor.rgb;
    // Preserve the original painted colors, then add modest local contrast in linear light.
    paint=max(0,lerp(dot(paint,half3(.2126,.7152,.0722)).xxx,paint,_Saturation));
    paint=max(0,(paint-.18)*_Contrast+.18);
    // Opt-in per wall: preserve the drawn stone detail beneath irregular, low moss stains.
    if(_FootMoss.z>0)
    {
     float along=i.world.x*.73+i.world.z*.89+_FootMoss.w;
     float patch=.5+.25*sin(along*1.7)+.15*sin(along*4.3+i.world.y*3.1)+.1*sin(along*9.2-i.world.y*5.7);
     float reach=max(.05,_FootMoss.y*(.25+.75*patch));
     float low=1-smoothstep(0,reach,max(0,i.world.y-_FootMoss.x));
     float moss=saturate(low*patch*_FootMoss.z)*(1-abs(normalize(i.normal).y));
     paint=lerp(paint,paint*half3(.48,.68,.32),moss);
    }
    if(_EdgeWidth>0)paint*=lerp(.42,1,smoothstep(0,max(_EdgeWidth,fwidth(edge)*1.5),edge));
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    half diffuse=saturate(dot(normalize(i.normal),sun.direction));
    half shade=lerp(_ShadowFloor,1,sun.shadowAttenuation);
    // Aurenfeld has golden daylight: cream sky fill and warm earth bounce keep shadows welcoming.
    half skyWeight=saturate(normalize(i.normal).y*.5+.5);
    half3 ambient=lerp(half3(.88,.73,.50),half3(1.02,.97,.84),skyWeight)*_AmbientFill;
    // Local art direction uses the same sun, surface normals and occlusion as the scene.
    // Default strength is zero, so shared materials and other scenes keep their lighting.
    half accent=(1-smoothstep(.15,1,distance(i.world,_MorningAccent.xyz)/max(.01,_MorningAccent.w)))
        *_MorningAccentStrength*sun.shadowAttenuation;
    half3 light=ambient+sun.color*(_SunStrength*diffuse*(shade+accent));
    return half4(MixFog(paint*light,i.fog),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster"
   Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma vertex ShadowPassVertex
   #pragma fragment ShadowPassFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly"
   Tags {"LightMode"="DepthOnly"}
   ZWrite On
   ColorMask 0
   HLSLPROGRAM
   #pragma multi_compile_instancing
   #pragma vertex DepthVert
   #pragma fragment DepthFrag
   struct DepthA {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct DepthV {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   DepthV DepthVert(DepthA i){DepthV o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
   half4 DepthFrag(DepthV i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);ClipViewCutaway(i.world);return 0;}
   ENDHLSL
  }
 }
}
