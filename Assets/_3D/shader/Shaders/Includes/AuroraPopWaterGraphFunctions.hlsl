#ifndef AURORAPOP_WATER_GRAPH_FUNCTIONS_INCLUDED
#define AURORAPOP_WATER_GRAPH_FUNCTIONS_INCLUDED

// Shader Graph Custom Function helpers for Aurora Pop stylized water.
// Use in Shader Graph Custom Function nodes with Source = File.
// Unity 6 / URP compatible HLSL math only.

void AuroraPopCausticParallax_float(
    float3 WorldPos,
    float3 CameraDirWS,
    float SceneDepth01,
    float SurfaceDepth01,
    float DepthScale,
    float ParallaxStrength,
    float Tiling,
    float2 SpeedA,
    float2 SpeedB,
    float Time,
    out float2 UV_A,
    out float2 UV_B,
    out float WaterDepth)
{
    WaterDepth = saturate((SceneDepth01 - SurfaceDepth01) * DepthScale);
    float2 baseUV = WorldPos.xz * Tiling;
    float2 parallax = normalize(CameraDirWS.xz + 1e-4) * WaterDepth * ParallaxStrength;
    UV_A = baseUV + parallax + SpeedA * Time;
    UV_B = baseUV * 1.37 - parallax * 0.62 + SpeedB * Time;
}

void AuroraPopFoamMask_float(
    float SceneDepth01,
    float SurfaceDepth01,
    float FoamDistance,
    float FoamPower,
    float Noise,
    float NoiseAmount,
    out float Foam)
{
    float d = max(SceneDepth01 - SurfaceDepth01, 0.00001);
    float edge = 1.0 - saturate(d / max(FoamDistance, 0.00001));
    Foam = saturate(pow(edge, FoamPower) + (Noise - 0.5) * NoiseAmount);
}

void AuroraPopWaterColor_float(
    float WaterDepth,
    float3 ShallowColor,
    float3 DeepColor,
    float3 FresnelColor,
    float Fresnel,
    float Caustic,
    float Foam,
    float3 FoamColor,
    out float3 BaseColor,
    out float Alpha)
{
    float3 c = lerp(ShallowColor, DeepColor, saturate(WaterDepth));
    c += FresnelColor * Fresnel;
    c += Caustic.xxx;
    c = lerp(c, FoamColor, Foam);
    BaseColor = c;
    Alpha = saturate(0.55 + WaterDepth * 0.35 + Fresnel * 0.15);
}

#endif
