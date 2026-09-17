//
//  OutlineMask.shader
//  QuickOutline
//
//  Created by Chris Nolet on 2/21/18.
//  Copyright © 2018 Chris Nolet. All rights reserved.
//

Shader "Custom/Outline Mask" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+100"
      "RenderType" = "Transparent"
    }

    Pass {
      Name "Mask"
      Cull Off
      ZTest [_ZTest]
      ZWrite Off
      ColorMask 0

      Stencil {
        Ref 1
        Pass Replace
      }
      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      struct Attributes {
        float4 position : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };
      struct Varyings { float4 position : SV_POSITION; };
      Varyings vert(Attributes input) {
        UNITY_SETUP_INSTANCE_ID(input);
        Varyings output;
        output.position = TransformObjectToHClip(input.position.xyz);
        return output;
      }
      half4 frag(Varyings input) : SV_Target { return 0; }
      ENDHLSL
    }
  }
}
