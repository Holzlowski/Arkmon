Shader "Arkmon/Illustrated Wardrobe"
{
    Properties
    {
        _BaseColor ("Paint tint", Color) = (1,1,1,1)
        _Panels ("Draw door panels", Float) = 1
        _Fabric ("Paint fabric", Float) = 0
        _PaintMap ("Fabric pattern", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_PaintMap); SAMPLER(sampler_PaintMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Panels;
                float _Fabric;
                float4 _PaintMap_ST;
            CBUFFER_END
            V Vert(A input) { V o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; return o; }
            float Box(float2 p, float2 size, float r)
            {
                float2 q=abs(p)-size+r;
                return length(max(q,0))+min(max(q.x,q.y),0)-r;
            }
            float Fill(float d) { float w=max(fwidth(d),.001); return 1-smoothstep(-w,w,d); }
            half4 Frag(V input) : SV_Target
            {
                if (_Fabric > .5)
                {
                    half3 cloth = SAMPLE_TEXTURE2D(_PaintMap, sampler_PaintMap, TRANSFORM_TEX(input.uv, _PaintMap)).rgb;
                    half3 tones = floor(saturate(cloth) * 8 + .5) / 8;
                    float fold = .82 + .18 * smoothstep(-.45,.65,sin(input.uv.x * 38 + sin(input.uv.y * 7)));
                    return half4(lerp(cloth, tones, .45) * fold * _BaseColor.rgb, 1);
                }
                float2 p=input.uv*float2(1,2.71);
                p.x+=.003*sin(p.y*35)+.002*sin(p.y*71);
                float stroke=smoothstep(.82,.98,sin(p.x*49+sin(p.y*5)*1.4))*(.55+.45*sin(p.y*17+p.x*21));
                half3 ink=half3(.065,.028,.016);
                half3 oak=lerp(half3(.25,.105,.048),half3(.40,.183,.079),1-p.x)+stroke*half3(.027,.013,.006);
                half3 color=lerp(ink,oak,Fill(Box(p-float2(.5,1.355),float2(.473,1.325),.025)));
                if (_Panels < .5)
                {
                    float edge = Fill(Box(input.uv-.5,float2(.482,.482),.023));
                    return half4(lerp(ink,oak,edge)*_BaseColor.rgb,1);
                }
                float2 center=float2(.5,p.y>1.38?1.99:.68);
                float d=Box(p-center,float2(.356,.56),.11);
                color=lerp(color,ink,Fill(d));
                color=lerp(color,half3(.51,.245,.095),Fill(d+.020));
                color=lerp(color,ink,Fill(d+.066));
                half3 panel=lerp(half3(.17,.064,.032),half3(.32,.131,.057),saturate(p.y-center.y+.5));
                color=lerp(color,panel+stroke*half3(.034,.015,.008),Fill(d+.082));
                float2 star=p-float2(.5,2.10);
                float crossShape=min(abs(star.x)+abs(star.y)*.34,abs(star.y)+abs(star.x)*.48);
                color=lerp(color,half3(.70,.43,.15),Fill(max(crossShape-.039,length(star)-.106)));
                return half4(color*_BaseColor.rgb,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
