Shader "Giby Games/Giby Ramp Transparent Instanced" {
	Properties {
		[KeywordEnum(Diffuse, Specular, SpecularRim)] _ShadingMode ("Shading Mode", Int) = 0
		_Color ("Color", Vector) = (1,1,1,1)
		_HColor ("Highlight Color", Vector) = (0.785,0.785,0.785,1)
		_SColor ("Shadow Color", Vector) = (0.195,0.195,0.195,1)
		_MainTex ("Main Texture", 2D) = "white" {}
		_RampThreshold ("Ramp Threshold", Range(0, 1)) = 0.5
		_RampSmooth ("Ramp Smoothing", Range(0.001, 1)) = 1
		_SpecColor ("Specular Color", Vector) = (0.5,0.5,0.5,1)
		_Smoothness ("Size", Float) = 0.2
		_SpecSmooth ("Smoothness", Range(0, 1)) = 1
		_ReflectionCubeMap ("Reflection Cube Map", Cube) = "" {}
		_ReflectionDensity ("Reflection Density", Range(0, 1)) = 0
		_RimColor ("Rim Color", Vector) = (1,1,1,1)
		_RimMin ("Rim Min", Range(0, 1)) = 1
		_RimMax ("Rim Max", Range(0, 5)) = 1
		ENABLE_EMISSION ("Emission", Int) = 0
		_EmissionColor ("Emission Color", Vector) = (1,1,1,0)
		_EmissionMap ("Emission Map", 2D) = "white" {}
		ENABLE_NORMAL ("Normal", Int) = 0
		[Normal] _BumpMap ("Normal map (RGB)", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Float) = 1
		ENABLE_DOUBLE_COLOR ("Double Color", Int) = 0
		_SecondaryColor ("Secondary Color", Vector) = (1,1,1,1)
		_DoubleColorMap ("Color Map", 2D) = "white" {}
		ENABLE_AO ("Ambient Occlusion", Int) = 0
		_AOColor ("AO Color", Vector) = (1,1,1,1)
		_AOTex ("AO Texture", 2D) = "white" {}
		ENABLE_DISSOLVE ("Dissolve", Int) = 0
		_DissolveColor ("Dissolve Color", Vector) = (1,1,1,1)
		_DissolveTex ("Dissolve Texture", 2D) = "white" {}
		_DissolveValue ("Dissolve Value", Range(0, 1)) = 0.5
		_DissolveGradientWidth ("Dissolve Gradient Width", Range(0, 1)) = 0.2
		ENABLE_DETAIL ("Detail", Int) = 0
		_DetailColor ("Detail Color", Vector) = (1,1,1,1)
		_DetailTex ("Detail Texture", 2D) = "black" {}
		[MaterialToggle] _Desaturable ("Desaturatable", Float) = 0
		[Header(Stencil)] [Space] _Stencil ("Stencil ID [0;255]", Float) = 0
		_ReadMask ("ReadMask [0;255]", Float) = 255
		_WriteMask ("WriteMask [0;255]", Float) = 255
		[Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
		[Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0
		[Header(Depth)] [Space] [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestMode ("ZTest", Float) = 4
		[Header(FOG)] [Space] [MaterialToggle] _FogEnabled ("FogEnabled", Float) = 1
		[HideInInspector] __dummy__ ("unused", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float4 _Color;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * _Color;
			}

			ENDHLSL
		}
	}
	Fallback "Diffuse"
	//CustomEditor "GibyRampGUI"
}