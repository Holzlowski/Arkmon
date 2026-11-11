
#ifndef ARKMON_STABLE_GROUND_BLEND_INCLUDED
#define ARKMON_STABLE_GROUND_BLEND_INCLUDED

float StableGroundBlend(float2 position)
{
    float2 enter = smoothstep(float2(-21,-15),float2(-17,-11),position);
    float2 exit = 1 - smoothstep(float2(-3,4),float2(0,7),position);
    const float textureStrength = 0.65;
    return textureStrength * enter.x * enter.y * exit.x * exit.y;
}

#endif
