Shader "AuroraPop/Stylized URP Water"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow Color", Color) = (0.14, 0.86, 1.0, 0.72)
        _DeepColor ("Deep Color", Color) = (0.0, 0.22, 0.95, 0.9)
        _FoamColor ("Foam Color", Color) = (0.95, 1.0, 1.0, 1.0)
        _FresnelColor ("Rim/Fresnel Color", Color) = (0.6, 1.0, 1.0, 1.0)
        _Alpha ("Global Alpha", Range(0,1)) = 0.82

        [Header(Depth)]
        _DepthMaxDistance ("Depth Max Distance", Range(0.01, 20)) = 4.0
        _DepthContrast ("Depth Contrast", Range(0.2, 4)) = 1.4
        _FoamDepth ("Foam Depth Distance", Range(0.001, 2)) = 0.22
        _FoamSoftness ("Foam Softness", Range(0.001, 2)) = 0.16

        [Header(Waves)]
        _WaveStrength ("Vertex Wave Height", Range(0, 0.5)) = 0.055
        _WaveScale ("Vertex Wave Scale", Range(0.1, 20)) = 3.2
        _WaveSpeed ("Vertex Wave Speed", Range(0, 5)) = 1.15
        _NormalStrength ("Fake Normal Strength", Range(0, 2)) = 0.85

        [Header(Foam Pattern)]
        _FoamNoiseScale ("Foam Noise Scale", Range(0.1, 80)) = 18
        _FoamNoiseSpeed ("Foam Noise Speed", Range(0, 5)) = 0.55
        _FoamCutoff ("Foam Cutoff", Range(0, 1)) = 0.48

        [Header(Sparkle Caustics)]
        _CausticColor ("Caustic Color", Color) = (0.8, 1.0, 1.0, 1.0)
        _CausticScale ("Caustic Scale", Range(1, 80)) = 22
        _CausticSpeed ("Caustic Speed", Range(0, 5)) = 0.7
        _CausticStrength ("Caustic Strength", Range(0, 2)) = 0.35

        [Header(Advanced)]
        [Toggle(_USE_OPAQUE_TEX)] _UseOpaqueTexture ("Use Opaque Texture Refraction", Float) = 1
        _RefractionStrength ("Refraction Strength", Range(0, 0.08)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "StylizedWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_OPAQUE_TEX
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #if defined(_USE_OPAQUE_TEX)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half4 _FresnelColor;
                half _Alpha;
                half _DepthMaxDistance;
                half _DepthContrast;
                half _FoamDepth;
                half _FoamSoftness;
                half _WaveStrength;
                half _WaveScale;
                half _WaveSpeed;
                half _NormalStrength;
                half _FoamNoiseScale;
                half _FoamNoiseSpeed;
                half _FoamCutoff;
                half4 _CausticColor;
                half _CausticScale;
                half _CausticSpeed;
                half _CausticStrength;
                half _UseOpaqueTexture;
                half _RefractionStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float eyeDepth     : TEXCOORD4;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                [unroll] for (int i = 0; i < 4; i++)
                {
                    v += a * valueNoise(p);
                    p = p * 2.02 + 17.17;
                    a *= 0.5;
                }
                return v;
            }

            float waveHeight(float2 xz, float t)
            {
                float w1 = sin((xz.x + xz.y * 0.55) * _WaveScale + t * _WaveSpeed);
                float w2 = sin((xz.y * 1.37 - xz.x * 0.42) * (_WaveScale * 1.7) - t * (_WaveSpeed * 1.25));
                return (w1 + 0.55 * w2) * _WaveStrength;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                posWS.y += waveHeight(posWS.xz, _Time.y);

                OUT.positionWS = posWS;
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.uv = IN.uv;
                OUT.eyeDepth = -TransformWorldToView(posWS).z;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float2 p = IN.positionWS.xz;
                float t = _Time.y;

                float n1 = fbm(p * _CausticScale * 0.05 + float2(t * _CausticSpeed, -t * _CausticSpeed * 0.7));
                float n2 = fbm(p * _CausticScale * 0.05 + float2(-t * _CausticSpeed * 0.6, t * _CausticSpeed));
                float caustics = smoothstep(0.62, 0.98, abs(n1 - n2) * 2.2);

                float dx = waveHeight(p + float2(0.03, 0), t) - waveHeight(p - float2(0.03, 0), t);
                float dz = waveHeight(p + float2(0, 0.03), t) - waveHeight(p - float2(0, 0.03), t);
                float3 fakeNormalWS = normalize(float3(-dx * _NormalStrength, 1.0, -dz * _NormalStrength));

                float sceneRawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float depthDiff = max(0.0, sceneEyeDepth - IN.eyeDepth);
                float depth01 = saturate(pow(depthDiff / max(0.001, _DepthMaxDistance), _DepthContrast));

                half4 waterCol = lerp(_ShallowColor, _DeepColor, depth01);

                #if defined(_USE_OPAQUE_TEX)
                    float2 refractUV = screenUV + fakeNormalWS.xz * _RefractionStrength * saturate(depthDiff);
                    half3 sceneCol = SampleSceneColor(refractUV).rgb;
                    waterCol.rgb = lerp(waterCol.rgb, sceneCol, 0.22 * saturate(depthDiff));
                #endif

                float foamEdge = 1.0 - smoothstep(_FoamDepth, _FoamDepth + _FoamSoftness, depthDiff);
                float foamNoise = fbm(p * _FoamNoiseScale * 0.12 + float2(t * _FoamNoiseSpeed, t * _FoamNoiseSpeed * 0.35));
                float foamMask = foamEdge * smoothstep(_FoamCutoff, _FoamCutoff + 0.18, foamNoise);

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float fresnel = pow(1.0 - saturate(dot(viewDirWS, fakeNormalWS)), 4.0);

                half3 color = waterCol.rgb;
                color += _CausticColor.rgb * caustics * _CausticStrength * (1.0 - depth01 * 0.35);
                color = lerp(color, _FoamColor.rgb, foamMask);
                color += _FresnelColor.rgb * fresnel * 0.35;

                half alpha = saturate(_Alpha + foamMask * 0.25 + fresnel * 0.18);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
