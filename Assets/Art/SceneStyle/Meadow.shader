Shader "Arkmon/Meadow"
{
    Properties { _ClearArea("Clear world rectangle: min X/Z, max X/Z", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float2 world:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            float4 _ClearArea;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.world=TransformObjectToWorld(input.positionOS.xyz).xz;
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                half shade=0.82+0.18*saturate(dot(normal,normalize(float3(-0.4,0.8,-0.3))));
                output.color=half4(input.color.rgb*shade,1);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target {
                if (_ClearArea.z > _ClearArea.x && _ClearArea.w > _ClearArea.y
                    && input.world.x > _ClearArea.x && input.world.x < _ClearArea.z
                    && input.world.y > _ClearArea.y && input.world.y < _ClearArea.w) discard;
                return half4(MixFog(input.color.rgb,input.fog),1);
            }
            ENDHLSL
        }
    }
}
