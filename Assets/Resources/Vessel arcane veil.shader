Shader "Arkmon/Vessel arcane veil"
{
    Properties
    {
        _Strength("Strength", Range(0, 1)) = 0
        _Origin("Vessel position and aspect", Vector) = (.5,.5,1.78,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _Strength;
            float4 _Origin;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screen : TEXCOORD0;
            };

            Varyings Vert(float4 positionOS : POSITION)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS.xyz);
                output.screen = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.screen.xy/input.screen.w;
                float2 distanceFromVessel = (uv-_Origin.xy)*float2(_Origin.z,1.2);
                float radius = length(distanceFromVessel);
                float drift = _Time.y*.72;
                float curl = sin(distanceFromVessel.x*3.2-distanceFromVessel.y*4.4+drift*.62);
                float phase = radius*15.5+curl*1.4-drift;
                float ribbon = pow(.5+.5*cos(phase),3);
                float hue = .5+.5*sin(phase*.48+distanceFromVessel.x*2.1+drift*.35);
                float3 color = lerp(float3(.23,.07,.62),float3(.035,.42,.47),hue);
                float spread = smoothstep(.09,.32,radius)*(1-.55*smoothstep(.83,1.3,radius));
                return half4(color,_Strength*spread*(.045+.34*ribbon));
            }
            ENDHLSL
        }
    }
}
