Shader "Arkmon/Meadow"
{
    Properties { _FogStrength("Distance haze strength",Range(0,1))=1 _SunResponse("Scene sunlight",Range(0,1))=0  _ClearArea("Clear world rectangle: min X/Z, max X/Z", Vector) = (0,0,0,0) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float2 world:TEXCOORD1; float3 positionWS:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
            float4 _ClearArea; float _SunResponse; float _FogStrength;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);output.world=output.positionWS.xz;
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                half shade=0.82+0.18*saturate(dot(normal,normalize(float3(-0.4,0.8,-0.3))));
                Light sun=GetMainLight();
                half3 morning=half3(.56,.61,.66)+sun.color*(.64*saturate(dot(normal,sun.direction)));
                output.color=half4(input.color.rgb*lerp(shade.xxx,morning,_SunResponse),1);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target {
                if (_ClearArea.z > _ClearArea.x && _ClearArea.w > _ClearArea.y
                    && input.world.x > _ClearArea.x && input.world.x < _ClearArea.z
                    && input.world.y > _ClearArea.y && input.world.y < _ClearArea.w) discard;
                half shadow=GetMainLight(TransformWorldToShadowCoord(input.positionWS)).shadowAttenuation;
                half3 lit=input.color.rgb*lerp(1,lerp(.60,1,shadow),_SunResponse);
                return half4(lerp(lit,MixFog(lit,input.fog),_FogStrength),1);
            }
            ENDHLSL
        }
    }
}
