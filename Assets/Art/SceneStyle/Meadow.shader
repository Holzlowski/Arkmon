Shader "Arkmon/Meadow"
{
    Properties { _HarvestCanopy("Painted wheat canopy",2D)="white" {} _CanopyStrength("Painted canopy strength",Range(0,1))=0  _FieldTexture("Painted grain texture",Range(0,1))=0  _FogStrength("Distance haze strength",Range(0,1))=1 _SunResponse("Scene sunlight",Range(0,1))=0  _ClearArea("Clear world rectangle: min X/Z, max X/Z", Vector) = (0,0,0,0) }
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
            #include "AurenfeldMorning.hlsl"
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half4 color:COLOR; half fog:TEXCOORD0; float2 world:TEXCOORD1; float3 positionWS:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
            float4 _ClearArea; float _CanopyStrength; float _SunResponse; float _FogStrength; float _FieldTexture;
            CBUFFER_END
            TEXTURE2D(_HarvestCanopy); SAMPLER(sampler_HarvestCanopy);
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);output.world=output.positionWS.xz;
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                half shade=0.82+0.18*saturate(dot(normal,normalize(float3(-0.4,0.8,-0.3))));
                Light sun=GetMainLight();
                half3 morning=half3(.56,.61,.66)+sun.color*(.64*saturate(dot(normal,sun.direction)));
                float field = _FieldTexture*smoothstep(46,58,-output.world.x);
                half3 directionalMorning=half3(.50,.54,.62)+sun.color*(.70*saturate(dot(normal,sun.direction)));
                float distant=smoothstep(175,220,-output.world.x);
                directionalMorning=lerp(directionalMorning,half3(.55,.56,.58)+sun.color*(.72*saturate(dot(normal,sun.direction))),distant);
                morning=lerp(morning,directionalMorning,field);
                output.color=half4(input.color.rgb*lerp(shade.xxx,morning,_SunResponse),1);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            float FieldNoise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                float a=frac(sin(dot(i,float2(127.1,311.7)))*43758.5453);
                float b=frac(sin(dot(i+float2(1,0),float2(127.1,311.7)))*43758.5453);
                float c=frac(sin(dot(i+float2(0,1),float2(127.1,311.7)))*43758.5453);
                float d=frac(sin(dot(i+1,float2(127.1,311.7)))*43758.5453);
                return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
            }
            half4 Frag(Varyings input):SV_Target {
                if (_ClearArea.z > _ClearArea.x && _ClearArea.w > _ClearArea.y
                    && input.world.x > _ClearArea.x && input.world.x < _ClearArea.z
                    && input.world.y > _ClearArea.y && input.world.y < _ClearArea.w) discard;
                half shadow=GetMainLight(TransformWorldToShadowCoord(input.positionWS)).shadowAttenuation;
                half fieldShadow=_FieldTexture*smoothstep(46,58,-input.world.x);
                half3 shadowTint=lerp(half3(.60,.60,.60),half3(.48,.54,.65),fieldShadow);
                half3 lit=input.color.rgb*lerp(half3(1,1,1),lerp(shadowTint,half3(1,1,1),shadow),_SunResponse);
                if(_FieldTexture>.5 && input.world.x < -46)
                {
                    float2 p=input.world;
                    float broad=FieldNoise(p*.12);
                    float angle=FieldNoise(p*.012)*1.4;
                    float stroke=dot(p,float2(sin(angle),cos(angle)))*3.5+sin(p.x*.04)*8+FieldNoise(p*.08)*2;
                    float resolved=1-smoothstep(.8,2.4,fwidth(stroke));
                    float rows=sin(stroke)*resolved;
                    float brush=FieldNoise(float2(p.x*.32+sin(p.y*.045),p.y*.95));
                    float brushAA=1-smoothstep(.8,2.0,max(fwidth(p.x*.32),fwidth(p.y*.95)));
                    float grainShade=.97+(broad-.5)*.13+rows*.012+(brush-.5)*.22*brushAA;
                    // In the distance, use clustered painted texture rather than long regular ribs.
                    float farField=smoothstep(175,230,-p.x);
                    float2 grainUV=float2(p.x*.68+sin(p.y*.024)*2,p.y*1.15+input.positionWS.y*.16);
                    float fleck=FieldNoise(grainUV);
                    float fleckAA=1-smoothstep(.8,2.4,max(fwidth(grainUV.x),fwidth(grainUV.y)));
                    float clusters=FieldNoise(p*.045);
                    float distantGrain=.99+(clusters-.5)*.10+(fleck-.5)*.20*fleckAA;
                    float gust=sin(p.x*.075+p.y*.052-_Time.y*.85);
                    distantGrain+=gust*.012;
                    grainShade=lerp(grainShade,distantGrain,farField);
                    lit*=lerp(1,grainShade,smoothstep(46,58,-p.x));
                    // Painted ears replace bare soil beyond the standing-grain foreground.
                    // Two softly warped scales prevent a conspicuous repeating tile grid.
                    float2 canopyUV=p*.062+float2(sin(p.y*.027),sin(p.x*.023))*.10;
                    half3 canopyA=SAMPLE_TEXTURE2D(_HarvestCanopy,sampler_HarvestCanopy,canopyUV).rgb;
                    half3 canopyB=SAMPLE_TEXTURE2D(_HarvestCanopy,sampler_HarvestCanopy,canopyUV*1.37+float2(.37,.61)).rgb;
                    half value=dot(lerp(canopyA,canopyB,.28),half3(.30,.59,.11));
                    half textureShade=clamp(value/.49,.62,1.55);
                    float canopyWeight=_CanopyStrength*smoothstep(105,170,-p.x);
                    lit*=lerp(1,textureShade,canopyWeight);
                }
                lit=AurenfeldMorningLight(lit,input.positionWS,0.35);
                return half4(lerp(lit,MixFog(lit,input.fog),_FogStrength),1);
            }
            ENDHLSL
        }
    }
}
