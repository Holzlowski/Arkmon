Shader "Arkmon/UI Bucket Contents"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Filled("Filled",Float)=0 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "UnityCG.cginc"
   struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex;fixed4 _Color;float _Filled;
   V Vert(A a){V o;o.position=UnityObjectToClipPos(a.vertex);o.uv=a.uv;o.color=a.color*_Color;return o;}
   fixed4 Frag(V i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv);
    if(c.g>c.r*1.12 && c.b>c.r*1.08){float light=dot(c.rgb,float3(.3,.59,.11));c.rgb=lerp(float3(.16,.065,.018)*(.9+light*.1),c.rgb,saturate(_Filled));}
    return c*i.color;
   }
   ENDCG
  }
 }
}