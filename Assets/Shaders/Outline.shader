// Контур активного шага: маска объекта в стенсиле + рамка вокруг силуэта.
Shader "ScenarioSystem/Outline"
{
	Properties
	{
		_OutlineColor ("Цвет", Color) = (0.1, 0.9, 0.1, 1)
		_OutlineWidth ("Толщина (м)", Float) = 0.15
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+100" }

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			half4 _OutlineColor;
			float _OutlineWidth;
		CBUFFER_END

		struct Attributes
		{
			float4 positionOS : POSITION;
			float3 normalOS : NORMAL;
		};

		struct Varyings
		{
			float4 positionCS : SV_POSITION;
		};
		ENDHLSL

		Pass
		{
			Name "OutlineMask"
			Tags { "LightMode" = "SRPDefaultUnlit" }
			Cull Off
			ZWrite Off
			ZTest LEqual
			ColorMask 0

			Stencil
			{
				Ref 1
				Pass Replace
			}

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			Varyings Vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return 0;
			}
			ENDHLSL
		}

		Pass
		{
			Name "OutlineFill"
			Tags { "LightMode" = "UniversalForward" }
			Cull Off
			ZWrite Off
			ZTest LEqual

			Stencil
			{
				Ref 1
				Comp NotEqual
			}

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			Varyings Vert(Attributes input)
			{
				Varyings output;
				float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
				// Направление от центра объекта к вершине (в мировых осях, с учетом масштаба);
				// вершина в самом центре — по нормали
				float3 fromCenterWS = TransformObjectToWorldDir(input.positionOS.xyz, false);
				float3 direction = dot(fromCenterWS, fromCenterWS) > 1e-6 ? fromCenterWS : TransformObjectToWorldDir(input.normalOS, false);
				output.positionCS = TransformWorldToHClip(positionWS + normalize(direction) * _OutlineWidth);
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				return _OutlineColor;
			}
			ENDHLSL
		}
	}
}
