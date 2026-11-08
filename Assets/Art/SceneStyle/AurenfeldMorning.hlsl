#ifndef ARKMON_AURENFELD_MORNING
#define ARKMON_AURENFELD_MORNING
// Scene-owned enable; zero outside the courtyard. All receivers share world-space weather.
float _AurenfeldMorning;
// XY is scene-owned wind drift in metres; Z controls cloud shade opacity.
float4 _AurenfeldClouds;
float AurenfeldCloudNoise(float2 p)
{
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    float a=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
    float b=frac(sin(dot(cell+float2(1,0),float2(127.1,311.7)))*43758.5453);
    float c=frac(sin(dot(cell+float2(0,1),float2(127.1,311.7)))*43758.5453);
    float d=frac(sin(dot(cell+1,float2(127.1,311.7)))*43758.5453);
    return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
}
float AurenfeldCloud(float3 p)
{
    // A moving world-space pattern, shared by soil, crops and paper vegetation.
    // Two scales form rounded cloud banks with irregular edges and clear sunlight gaps.
    float2 q=p.xz-_AurenfeldClouds.xy+float2(43,18);
    float cover=AurenfeldCloudNoise(q/38)*.78+AurenfeldCloudNoise(q/17+13.7)*.22;
    return smoothstep(.43,.59,cover)*_AurenfeldClouds.z;
}
half3 AurenfeldMorningLight(half3 color,float3 p,float tips)
{
    float weight=_AurenfeldMorning*smoothstep(48,78,-p.x);
    float foreground=1-smoothstep(78,142,-p.x);
    float farm=exp(-dot((p.xz-float2(-148,18))/float2(55,65),(p.xz-float2(-148,18))/float2(55,65)));
    float cloud=AurenfeldCloud(p)*(1-farm*.18);
    half3 tint=lerp(half3(1.06,1.015,.93),half3(.38,.49,.67),cloud);
    tint*=1-foreground*.13+farm*.15;
    float wave=sin(p.x*.075+p.z*.052-_Time.y*.85);
    tint+=half3(.085,.063,.025)*pow(saturate(wave),3)*tips*(1-cloud);
    color*=lerp(half3(1,1,1),tint,weight);
    // Restrained warmth in two low swales, never a distance veil over the mountains.
    float2 a=(p.xz-float2(-188,40))/float2(24,65);
    float2 b=(p.xz-float2(-281,-25))/float2(29,90);
    float low=exp(-dot(a,a))+exp(-dot(b,b));
    float mist=low*.065*(1-smoothstep(15,32,p.y))*weight;
    return lerp(color,half3(.78,.64,.44),saturate(mist));
}
#endif
