Shader "Arkmon/Blue Sky"
{
    Properties
    {
        _Horizon("Horizon", Color)=(0.4,0.68,0.93,1)
        _Zenith("Zenith", Color)=(0.08,0.34,0.72,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Horizon;
                half4 _Zenith;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.direction=input.positionOS.xyz; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 direction=normalize(input.direction);
                half3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(saturate(direction.y*2),0.6));
                float sun=saturate(dot(direction,normalize(float3(-0.3,0.65,-0.5))));
                color+=half3(1,0.88,0.63)*(pow(sun,500)*0.45+pow(sun,24)*0.035);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
