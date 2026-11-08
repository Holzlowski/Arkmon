// Review-only tilt-shift composition; not installed on the gameplay camera.
Shader "Hidden/Arkmon/Lookout Miniature Preview"
{
    Properties { _MainTex("Sharp view",2D)="white" {} _BlurredTex("Blurred view",2D)="white" {} _FocusBand("Sharp half-width, falloff, centre",Vector)=(.045,.22,.5,0) _Strength("Strength",Range(0,1))=1 _ForegroundStrength("Foreground strength",Range(0,1))=1 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BlurredTex;
            float4 _FocusBand;
            float _Strength;
            float _ForegroundStrength;
            fixed4 frag(v2f_img i):SV_Target
            {
                float band=_Strength*smoothstep(_FocusBand.x,_FocusBand.y,abs(i.uv.y-_FocusBand.z));
                band*=lerp(_ForegroundStrength,1,step(_FocusBand.z,i.uv.y));
                return lerp(tex2D(_MainTex,i.uv),tex2D(_BlurredTex,i.uv),band);
            }
            ENDCG
        }
    }
}
