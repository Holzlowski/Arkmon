#ifndef ARKMON_SUN_MOTION
#define ARKMON_SUN_MOTION
// 12/19-second cycles; fixed sources and 55-100% length keep motion inside authored bounds.
float BreachRayLength(float3 origin, float time)
{
    float phase=origin.z*.73+origin.x*.11;
    return .775+.175*sin(time*(6.2831853/12)+phase)+.05*sin(time*(6.2831853/19)+phase*1.7);
}
#endif
