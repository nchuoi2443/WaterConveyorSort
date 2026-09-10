Shader "AuroraPop/WaterChannelFlow_StylizedURP"
{
    Properties
    {
        [Header(Base)]
        _ShallowColor("Shallow Color", Color) = (0.16,0.86,1,0.78)
        _DeepColor("Deep Color", Color) = (0.02,0.34,0.95,0.88)
        _Opacity("Opacity", Range(0,1)) = 0.82
        _DepthDistance("Depth Distance", Range(0.01,8)) = 2.2
        _DepthPower("Depth Power", Range(0.2,5)) = 1.35

        [Header(Flow)]
        _FlowTex("Flow Streaks", 2D) = "white" {}
        _FlowSpeedX("Flow Speed X", Range(-5,5)) = -0.8
        _FlowTiling("Flow Tiling", Vector) = (2.2, 9.0, 0, 0)
        _FlowStrength("Flow Strength", Range(0,2)) = 0.65
        _FlowColor("Flow Highlight Color", Color) = (0.8,1,1,1)

        [Header(Caustics)]
        _CausticsTex("Caustics", 2D) = "white" {}
        _CausticsTiling("Caustics Tiling", Vector) = (2.5,2.5,0,0)
        _CausticsSpeed("Caustics Speed", Vector) = (0.05,-0.03,0,0)
        _CausticsStrength("Caustics Strength", Range(0,3)) = 1.1
        _CausticsParallax("Caustics Parallax Depth Push", Range(0,0.25)) = 0.055

        [Header(Foam)]
        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamColor("Foam Color", Color) = (0.92,1,1,1)
        _FoamStrength("Foam Strength", Range(0,3)) = 1.0
        _FoamSpeedX("Foam Speed X", Range(-5,5)) = -1.5
        _FoamEdgeWidth("Foam Depth Edge Width", Range(0.01,4)) = 0.35
        _FoamTiling("Foam Tiling", Vector) = (4,8,0,0)

        [Header(Normal Refraction)]
        _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalTiling("Normal Tiling", Vector) = (2,2,0,0)
        _NormalSpeed("Normal Speed", Vector) = (-0.04,0.03,0,0)
        _NormalStrength("Normal Strength", Range(0,1)) = 0.18
        _Distortion("Screen Refraction", Range(0,0.05)) = 0.012

        [Header(Final)]
        _RimGlow("Rim Glow", Range(0,1)) = 0.25
        _Brightness("Brightness", Range(0,2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_FlowTex); SAMPLER(sampler_FlowTex);
            TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);

            CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor, _DeepColor, _FlowColor, _FoamColor;
            float _Opacity, _DepthDistance, _DepthPower;
            float _FlowSpeedX, _FlowStrength;
            float4 _FlowTiling;
            float4 _CausticsTiling, _CausticsSpeed;
            float _CausticsStrength, _CausticsParallax;
            float _FoamStrength, _FoamSpeedX, _FoamEdgeWidth;
            float4 _FoamTiling;
            float4 _NormalTiling, _NormalSpeed;
            float _NormalStrength, _Distortion, _RimGlow, _Brightness;
            CBUFFER_END

            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; };
            struct Varyings { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; float4 screenPos: TEXCOORD1; float3 positionWS: TEXCOORD2; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float t = _Time.y;
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneRaw = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float thisEye = LinearEyeDepth(IN.positionCS.z / IN.positionCS.w, _ZBufferParams);
                float depthDiff = max(0, sceneEye - thisEye);
                float depth01 = saturate(pow(depthDiff / max(0.0001, _DepthDistance), _DepthPower));
                float foamEdge = 1.0 - saturate(depthDiff / max(0.0001, _FoamEdgeWidth));

                float2 nUV = uv * _NormalTiling.xy + _NormalSpeed.xy * t;
                half3 n = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, nUV), _NormalStrength);
                float2 distort = n.xy * _Distortion * (0.35 + depth01);
                half3 sceneCol = SampleSceneColor(screenUV + distort).rgb;

                half3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth01);
                float2 flowUV1 = uv * _FlowTiling.xy + float2(_FlowSpeedX * t, 0);
                float2 flowUV2 = uv * (_FlowTiling.xy * 1.63) + float2(_FlowSpeedX * -0.53 * t + 0.37, 0.07 * t);
                half flowA = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, flowUV1).a;
                half flowB = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, flowUV2).a;
                half flow = saturate(flowA + flowB * 0.65) * _FlowStrength;

                float2 causticUV = uv * _CausticsTiling.xy + _CausticsSpeed.xy * t + n.xy * _CausticsParallax * (1.0 + depth01 * 1.8);
                half ca = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticUV).a;
                ca *= _CausticsStrength * saturate(1.25 - depth01 * 0.55);

                float2 foamUV = uv * _FoamTiling.xy + float2(_FoamSpeedX * t, 0.13 * t);
                half foamTex = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV).a;
                half foam = foamTex * foamEdge * _FoamStrength;

                half3 col = baseCol;
                col += _FlowColor.rgb * flow * 0.34;
                col += half3(0.75, 1.0, 1.0) * ca * 0.55;
                col = lerp(col, _FoamColor.rgb, saturate(foam));
                col = lerp(sceneCol, col, _Opacity);
                col += pow(1.0 - saturate(depth01), 2.5) * _RimGlow * half3(0.4,0.95,1.0);
                col *= _Brightness;
                half alpha = saturate(_Opacity + foam * 0.15);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
