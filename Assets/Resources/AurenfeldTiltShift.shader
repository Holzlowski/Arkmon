Shader "Hidden/Arkmon/Aurenfeld Tilt Shift"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" }
  Cull Off ZWrite Off ZTest Always
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
   float4 _FocusBand; // sharp half-width, falloff, viewport centre, blend
   float4 _Blur; // radius in normalized x/y, foreground multiplier
   half4 Read(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv); }
   half4 Frag(Varyings i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 uv=i.texcoord;
    float y=uv.y;
    // Blit UVs already match the camera colour orientation; do not flip them again.
    float band=smoothstep(_FocusBand.x,_FocusBand.y,abs(y-_FocusBand.z));
    float blend=band*_FocusBand.w*lerp(_Blur.z,1,step(_FocusBand.z,y));
    half4 sharp=Read(uv);
    // Avoid sampling the neighbourhood inside the protected farm band.
    if(blend<.001)return sharp;
    float2 d=_Blur.xy*lerp(.35,1,band);
    half4 soft=sharp*4;
    soft+=2*(Read(uv+float2(d.x,0))+Read(uv-float2(d.x,0))
           +Read(uv+float2(0,d.y))+Read(uv-float2(0,d.y)));
    soft+=Read(uv+d)+Read(uv-d)+Read(uv+float2(d.x,-d.y))+Read(uv+float2(-d.x,d.y));
    soft+=Read(uv+float2(d.x*2,0))+Read(uv-float2(d.x*2,0))
         +Read(uv+float2(0,d.y*2))+Read(uv-float2(0,d.y*2));
    return lerp(sharp,soft/20,blend);
   }
   ENDHLSL
  }
 }
}
