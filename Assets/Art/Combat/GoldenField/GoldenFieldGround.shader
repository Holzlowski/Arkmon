Shader "Arkmon/Golden Field Ground"
{
    Properties
    {
        _BaseMap("Earth texture", 2D) = "white" {}
        _BaseColor("Earth tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../../SceneStyle/AurenfeldMorning.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _FieldCenter, _SealGround;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.world);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 world = input.world.xz;
                float broad = AurenfeldCloudNoise(world * .084 + 7.1);
                float patches = AurenfeldCloudNoise(world * .37 - 3.8);
                float2 warped = world + 1.1 * float2(broad - .5, patches - .5);
                float2 rotated = float2(dot(warped, float2(.8,.6)), dot(warped, float2(-.6,.8)));
                half3 largeGrain = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, warped * .13).rgb;
                half3 smallGrain = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, rotated * .22 + .38).rgb;
                half3 grit = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, world * .71 + .17).rgb;
                half3 color = lerp(largeGrain, smallGrain, lerp(.34,.78,smoothstep(.21,.78,patches))) * _BaseColor.rgb;
                color *= lerp(.85, 1.12, broad) * lerp(.92, 1.10, patches) * (.88 + grit * .19);
                float2 fieldPoint = input.world.xz - _FieldCenter.xy;
                float2 q = abs(fieldPoint + float2(0, 1)) / float2(13.8, 13.5);
                float edge = pow(dot(q*q, q*q), .25)
                    + .045 * sin(fieldPoint.x * .85 + fieldPoint.y * .49) + .035 * sin(fieldPoint.y * .9 - fieldPoint.x * .48);
                float pathZ = 14.6 + 2.8 * sin(fieldPoint.x * .145) + .009 * fieldPoint.x * fieldPoint.x;
                float path = abs(fieldPoint.y - pathZ);
                half cropped = (1 - smoothstep(.76, 1.13, edge));
                half worn = 1 - smoothstep(.65, 1.95, path + .10 * sin(fieldPoint.x * 3.2));
                color *= lerp(half3(1,.99,.92), half3(.81,.68,.50), cropped);
                color *= lerp(1, .73, worn);
                color = AurenfeldMorningLight(color, input.world, 0);
                // The ward is drawn into the soil, so its edge always follows the ground.
                float2 fromSeal = input.world.xz - _SealGround.xy;
                float radius = length(fromSeal);
                float angle = atan2(fromSeal.y, fromSeal.x);
                float ring = 1 - smoothstep(.014,.052,min(abs(radius-1.87),abs(radius-1.48)));
                float inner = (1 - smoothstep(.018,.057,abs(radius-.81))) * .65;
                float ticks = 1 - smoothstep(.06,.24,abs(sin(angle*12)));
                ticks *= smoothstep(1.51,1.59,radius) * (1-smoothstep(1.72,1.83,radius));
                float star = 1 - smoothstep(.02,.07,abs(sin(angle*4))*radius);
                star *= (1-smoothstep(.62,1.01,radius))*smoothstep(.07,.18,radius);
                float shadow = (1-smoothstep(.33,1.87,radius)) * _SealGround.z;
                float spill = (1-smoothstep(1.38,2.7,radius)) * _SealGround.z;
                half inlay = (1-smoothstep(1.86,1.94,radius))*_SealGround.z;
                half seam = (1-smoothstep(.007,.025,abs(sin(angle*8))))*smoothstep(1.50,1.59,radius);
                half3 slate = half3(.20,.115,.17) * (.68 + grit * .48) * (1-seam*.43);
                slate = AurenfeldMorningLight(slate,input.world,0);
                color = lerp(color * (1-shadow*.22),slate,inlay) + half3(.105,.027,.21)*spill;
                color += (half3(.80,.26,1.32)*(ring+inner) + half3(1.12,.62,.18)*(ticks+star*.53)) * _SealGround.z;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}