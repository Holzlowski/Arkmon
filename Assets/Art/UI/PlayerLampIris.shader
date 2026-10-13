Shader "Arkmon/Player Lamp Iris"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Openness ("Openness", Float) = 1
        _Aspect ("Aspect", Float) = 1.7778
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            float4 _Color; float _Openness,_Aspect;
            Varyings vert(Attributes v) { Varyings o; o.vertex=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;o.color=v.color*_Color;return o; }
            float ellipse(float2 p,float2 centre,float2 radius) {return length((p-centre)/radius)-1;}
            half4 frag(Varyings i):SV_Target
            {
                if(_Openness<=.001)return i.color;
                float scale=sqrt(_Aspect*_Aspect+1)*3.5*_Openness;
                float2 p=(i.uv-.5)*float2(_Aspect,1)/scale; p.y+=.14;
                // Round lantern body, foot, cap, top loop and a separate side grip.
                float body=ellipse(p,float2(0,-.035),float2(.28,.31));
                body=min(body,ellipse(p,float2(0,-.345),float2(.28,.065)));
                body=min(body,ellipse(p,float2(0,.275),float2(.235,.065)));
                body=min(body,ellipse(p,float2(0,.355),float2(.08,.09)));
                float loop=max(ellipse(p,float2(0,.515),float2(.115,.155)),-ellipse(p,float2(0,.525),float2(.061,.085)));
                float handle=max(ellipse(p,float2(-.265,-.04),float2(.13,.22)),-ellipse(p,float2(-.275,-.04),float2(.07,.135)));
                float distance=min(body,min(loop,handle));
                float edge=max(fwidth(distance),.001);
                i.color.a*=smoothstep(-edge,edge,distance);
                return i.color;
            }
            ENDHLSL
        }
    }
}
