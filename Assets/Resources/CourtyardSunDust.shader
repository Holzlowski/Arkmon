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
   #include "CourtyardSunMotion.hlsl"
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 world:TEXCOORD1; };
   CBUFFER_START(UnityPerMaterial)
   half _Visibility;
   float4 _BeamStarts[9];
   float4 _BeamEnds[9];
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
    float beam=0;
    [unroll] for(int n=0;n<9;n++)
    {
     float lengthScale=BreachRayLength(_BeamStarts[n].xyz,_Time.y);
     float3 axis=(_BeamEnds[n].xyz-_BeamStarts[n].xyz)*lengthScale;
     float along=saturate(dot(i.world-_BeamStarts[n].xyz,axis)/max(.001,dot(axis,axis)));
     float width=lerp(_BeamStarts[n].w,_BeamEnds[n].w,along)*lengthScale+.25;
     float lit=1-smoothstep(width*.35,width+.5,distance(i.world,_BeamStarts[n].xyz+along*axis));
     float phase=_BeamStarts[n].z*.73+_BeamStarts[n].x*.11;
     float cloud=.5+.5*sin(_Time.y*.22+phase+.45*sin(_Time.y*.13+phase*2));
     beam=max(beam,lit*(.48+.52*smoothstep(.1,.9,cloud))*smoothstep(0,.06,along)*(1-smoothstep(.75,1,along)));
    }
    float nearFade=smoothstep(.3,1.5,distance(i.world,_WorldSpaceCameraPos));
    return half4(i.color.rgb,i.color.a*speck*beam*nearFade*_Visibility);
   }
   ENDHLSL
  }
 }
}
