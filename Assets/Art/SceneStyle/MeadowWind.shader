Shader "Arkmon/Meadow Wind"
{
    Properties
    {
        _SunResponse("Scene sunlight",Range(0,1))=0
        _WindStrength("Wind strength",Float)=.16
        _FieldStyle("Aurenfeld field waves",Range(0,1))=0
        _GrassShadowCamera("Near shadow camera",Vector)=(0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _WindStrength, _SunResponse, _FieldStyle;
            float4 _GrassShadowCamera;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
        // Both render passes use the same world-space deformation; roots stay fixed.
        float3 MeadowPosition(Attributes input)
        {
            float3 p=TransformObjectToWorld(input.positionOS.xyz);
            float oldWave=sin(p.x*.13+p.z*.17+_Time.y*1.4)+sin(p.z*.3-_Time.y*.8)*.35;
            float sweep=sin(p.x*.075+p.z*.052-_Time.y*.85);
            float swell=.55+.45*sin(p.x*.028-p.z*.035-_Time.y*.32);
            float flutter=sin(_Time.y*2.1+input.uv.x*6.283+p.x*.2)*.12;
            float wave=lerp(oldWave,.35+sweep*(.85+swell*.45)+flutter,_FieldStyle);
            float field=_FieldStyle*smoothstep(46,58,-p.x);
            float bend=wave*_WindStrength*lerp(1,1.3,field)*input.uv.y*input.uv.y;
            p.x+=bend;p.z+=bend*lerp(.3,.42,_FieldStyle);
            return p;
        }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;float3 position=MeadowPosition(input);
                output.positionCS=TransformWorldToHClip(position);output.positionWS=position;
                Light sun=GetMainLight();
                half3 morning=half3(.44,.50,.60)+sun.color*(.48*lerp(.55,1,input.uv.y));
                float field=_FieldStyle*smoothstep(46,58,-position.x);
                half3 fieldLight=half3(.34,.40,.51)+sun.color*(.56*lerp(.45,1,input.uv.y));
                morning=lerp(morning,fieldLight,field);
                float sweep=sin(position.x*.075+position.z*.052-_Time.y*.85);
                half shimmer=1+field*sweep*.055*input.uv.y;
                output.color=half4(input.color.rgb*lerp(half3(1,1,1),morning,_SunResponse)*shimmer,input.color.a);
                output.fog=ComputeFogFactor(output.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(input.positionCS);
                #endif
                half shadow=GetMainLight(TransformWorldToShadowCoord(input.positionWS)).shadowAttenuation;
                half field=_FieldStyle*smoothstep(46,58,-input.positionWS.x);
                half3 shade=lerp(half3(.58,.58,.58),half3(.46,.53,.65),field);
                return half4(MixFog(input.color.rgb*lerp(half3(1,1,1),lerp(shade,half3(1,1,1),shadow),_SunResponse),input.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            struct ShadowVaryings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float phase:TEXCOORD1; };
            ShadowVaryings ShadowVert(Attributes input)
            {
                ShadowVaryings o;float3 p=MeadowPosition(input);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection=normalize(_LightPosition-p);
                #else
                float3 lightDirection=_LightDirection;
                #endif
                o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(p,float3(0,1,0),lightDirection)));
                o.world=p;o.phase=input.uv.x;return o;
            }
            half4 ShadowFrag(ShadowVaryings i):SV_TARGET
            {
                #if defined(LOD_FADE_CROSSFADE)
                LODFadeCrossFade(i.positionCS);
                #endif
                // Whole tufts retire progressively, avoiding a hard circular shadow boundary.
                float fade=1-smoothstep(26,34,distance(i.world,_GrassShadowCamera.xyz));
                clip(_FieldStyle*_GrassShadowCamera.w*fade-max(.001,i.phase));
                return 0;
            }
            ENDHLSL
        }
    }
}
