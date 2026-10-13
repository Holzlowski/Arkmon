Shader "Arkmon/Painted castle"
{
 Properties {
 _BaseMap("Painted stone", 2D) = "white" {} _BaseColor("Tint", Color) = (1,1,1,1)
 _DetailStrength("Texture contrast", Range(0,1)) = 1
 _TextureScale("World texture scale", Float) = .35
 _WarmLight("Window warmth", Range(0,1)) = 0
 _MorningStrength("Tower morning contrast", Range(0,1)) = 0
 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
 struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; };
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;
float _DetailStrength, _TextureScale, _WarmLight, _MorningStrength;
 CBUFFER_END
 V vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(i.normalOS); return o; }
 half4 frag(V i):SV_Target {
 float3 n=normalize(i.normal), w=pow(abs(n),4); w/=max(dot(w,1),.001);
 float3 p=i.world*_TextureScale;
 half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x
 +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y
 +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
 c=lerp(half3(.72,.69,.61),c,_DetailStrength);
 // Soft painted light pool from the tower window, limited to upward-facing surfaces.
 float pool=exp(-pow((i.world.x+.7)/3.8,2)-pow((i.world.z-1.5)/4.5,2))*saturate(n.y)*_WarmLight;
 c=lerp(c,c*half3(1.16,1.08,.92),pool);
 // Keep the painterly shader, but let the tower's morning wash read on unlit stone.
 float sun=exp(-pow((i.world.x+1.7)/1.65,2)-pow((i.world.z-1.1)/2.4,2))*saturate(n.y);
 c*=lerp(half3(1,1,1),half3(.69,.78,.91),_MorningStrength*(1-sun));
 c*=1+_MorningStrength*sun*half3(.95,.50,-.08);
 // Thin warm air gathers toward the lit window, preserving the painted stone detail.
 float haze=_MorningStrength*saturate(distance(_WorldSpaceCameraPos,i.world)*.018)*smoothstep(0,6,i.world.z);
 c=lerp(c,half3(.94,.81,.59),haze*.55);
 return half4(c*_BaseColor.rgb*(.72+.28*saturate(dot(n,normalize(float3(-.4,.8,-.3))))),1);
 }
 ENDHLSL
 }
 }
}
