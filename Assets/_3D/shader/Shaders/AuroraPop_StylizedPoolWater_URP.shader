Shader "AuroraPop/Water/Stylized Pool Water URP"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor("Shallow Color", Color) = (0.12,0.95,1.0,0.65)
        _DeepColor("Deep Color", Color) = (0.00,0.28,0.95,0.85)
        _FoamColor("Foam Color", Color) = (0.85,1,1,1)
        _FresnelColor("Fresnel Rim Color", Color) = (0.7,1,1,1)
        _Opacity("Base Opacity", Range(0,1)) = 0.72
        _DepthOpacity("Depth Opacity", Range(0,1)) = 0.25

        [Header(Depth)]
        _DepthDistance("Depth Distance", Float) = 4.5
        _DepthPower("Depth Power", Range(0.25,4)) = 1.2
        _DeepPush("Caustic Deep Push", Range(0,2)) = 0.65

        [Header(Foam)]
        _FoamDistance("Foam Distance", Float) = 0.55
        _FoamPower("Foam Power", Range(0.5,8)) = 2.2
        _FoamAmount("Foam Amount", Range(0,1)) = 0.75
        _FoamNoiseTex("Foam Noise", 2D) = "white" {}
        _FoamNoiseTiling("Foam Noise Tiling", Float) = 2.5
        _FoamNoiseSpeed("Foam Noise Speed", Vector) = (0.02, 0.035, 0, 0)

        [Header(Caustics)]
        _CausticsTex("Caustics Texture", 2D) = "white" {}
        _CausticsColor("Caustics Color", Color) = (0.75,1,0.95,1)
        _CausticsTiling("Caustics Tiling", Float) = 1.8
        _CausticsSpeedA("Caustics Speed A", Vector) = (0.035, 0.018, 0, 0)
        _CausticsSpeedB("Caustics Speed B", Vector) = (-0.025, 0.032, 0, 0)
        _CausticsStrength("Caustics Strength", Range(0,3)) = 1.25
        _CausticsContrast("Caustics Contrast", Range(0.25,5)) = 2.4
        _ParallaxStrength("Caustic Parallax Strength", Range(0,2)) = 0.55

        [Header(Waves)]
        _WaveNormalA("Wave Normal A", 2D) = "bump" {}
        _WaveNormalB("Wave Normal B", 2D) = "bump" {}
        _WaveTilingA("Wave Tiling A", Float) = 1.4
        _WaveTilingB("Wave Tiling B", Float) = 3.1
        _WaveSpeedA("Wave Speed A", Vector) = (0.025, 0.02, 0, 0)
        _WaveSpeedB("Wave Speed B", Vector) = (-0.018, 0.03, 0, 0)
        _NormalStrength("Normal Strength", Range(0,2)) = 0.45
        _SparkleStrength("Sparkle Strength", Range(0,1)) = 0.2

        [Header(Refraction)]
        _RefractionStrength("Refraction Strength", Range(0,0.08)) = 0.018
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);
            TEXTURE2D(_FoamNoiseTex); SAMPLER(sampler_FoamNoiseTex);
            TEXTURE2D(_WaveNormalA); SAMPLER(sampler_WaveNormalA);
            TEXTURE2D(_WaveNormalB); SAMPLER(sampler_WaveNormalB);

            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor, _DeepColor, _FoamColor, _FresnelColor, _CausticsColor;
            float _Opacity, _DepthOpacity, _DepthDistance, _DepthPower, _DeepPush;
            float _FoamDistance, _FoamPower, _FoamAmount, _FoamNoiseTiling;
            float4 _FoamNoiseSpeed;
            float _CausticsTiling, _CausticsStrength, _CausticsContrast, _ParallaxStrength;
            float4 _CausticsSpeedA, _CausticsSpeedB;
            float _WaveTilingA, _WaveTilingB, _NormalStrength, _SparkleStrength, _RefractionStrength;
            float4 _WaveSpeedA, _WaveSpeedB;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
                float4 screenPos:TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = normalize(n.normalWS);
                OUT.screenPos = ComputeScreenPos(p.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float surfEye = LinearEyeDepth(IN.positionCS.z, _ZBufferParams);
                float waterDepth = max(sceneEye - surfEye, 0);
                float depth01 = saturate(pow(waterDepth / max(_DepthDistance, 0.001), _DepthPower));

                float2 uvWorld = IN.positionWS.xz;
                float2 nUVa = uvWorld * _WaveTilingA + _WaveSpeedA.xy * _Time.y;
                float2 nUVb = uvWorld * _WaveTilingB + _WaveSpeedB.xy * _Time.y;
                float3 na = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormalA, sampler_WaveNormalA, nUVa), _NormalStrength);
                float3 nb = UnpackNormalScale(SAMPLE_TEXTURE2D(_WaveNormalB, sampler_WaveNormalB, nUVb), _NormalStrength * 0.55);
                float3 normalTS = normalize(float3(na.xy + nb.xy, 1));
                float3 nWS = normalize(IN.normalWS + float3(normalTS.x, 0, normalTS.y) * _NormalStrength);

                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);
                float fresnel = pow(1.0 - saturate(dot(nWS, viewDir)), 4.0);

                // Parallax caustics: deeper pixels push caustic projection further along camera XZ direction.
                float2 parallax = normalize(viewDir.xz + 1e-4) * depth01 * _ParallaxStrength * (1.0 + _DeepPush);
                float2 cUVa = uvWorld * _CausticsTiling + parallax + _CausticsSpeedA.xy * _Time.y;
                float2 cUVb = uvWorld * (_CausticsTiling * 1.37) - parallax * 0.75 + _CausticsSpeedB.xy * _Time.y;
                float ca = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, cUVa).r;
                float cb = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, cUVb).g;
                float caustic = pow(saturate(ca * cb * 1.8), _CausticsContrast) * _CausticsStrength;
                caustic *= saturate(1.0 - depth01 * 0.55);

                float2 foamUV = uvWorld * _FoamNoiseTiling + _FoamNoiseSpeed.xy * _Time.y;
                float foamNoise = SAMPLE_TEXTURE2D(_FoamNoiseTex, sampler_FoamNoiseTex, foamUV).r;
                float foamEdge = 1.0 - saturate(waterDepth / max(_FoamDistance, 0.001));
                float foam = saturate(pow(foamEdge, _FoamPower) + (foamNoise - 0.5) * 0.25) * _FoamAmount;

                float3 col = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                col += _FresnelColor.rgb * fresnel * 0.55;
                col += _CausticsColor.rgb * caustic;
                float sparkle = step(0.985, foamNoise) * _SparkleStrength * (0.5 + fresnel);
                col += sparkle.xxx;
                col = lerp(col, _FoamColor.rgb, foam);

                float2 refrUV = screenUV + normalTS.xy * _RefractionStrength * (1.0 - foam) * saturate(waterDepth);
                float3 sceneCol = SampleSceneColor(refrUV).rgb;
                col = lerp(sceneCol, col, 0.72);

                float alpha = saturate(_Opacity + depth01 * _DepthOpacity + fresnel * 0.12 + foam * 0.2);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
