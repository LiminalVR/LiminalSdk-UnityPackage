// FFR-compatible color grading. Rendered as ordinary end-of-frame geometry
// (a fullscreen triangle) rather than an OnRenderImage blit, so the camera
// keeps rendering directly into the foveated XR swapchain — any OnRenderImage
// forces the scene into an unfoveated intermediate RT and defeats FFR.
//
// Reads the current pixel via framebuffer fetch (the inout SV_Target below),
// so no scene copy is needed. Supported by Adreno under GLES3 on Quest.
// NOT supported under Vulkan or D3D — if the platform moves off GLES3 this
// path must be revisited. With MSAA the fragment runs per-sample, which costs
// more on this one triangle but grades correctly.
Shader "Liminal/PostFX/ColorGradingFetch"
{
	Properties
	{
		_Brightness("Brightness", Range(0, 2)) = 1
		_Contrast("Contrast", Range(0, 2)) = 1
		_HueShift("Hue Shift (Degrees)", Range(-180, 180)) = 0
		_Saturation("Saturation", Range(0, 2)) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Overlay" "RenderType" = "Overlay" "IgnoreProjector" = "True" }

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma only_renderers gles3 metal
			#include "UnityCG.cginc"
			#include "ColorGrade.cginc"

			half _Brightness;
			half _Contrast;
			half _HueShift;
			half _Saturation;

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				// Mesh vertices are authored directly in clip space (fullscreen
				// triangle); bypassing the camera matrices guarantees coverage
				// in both eyes regardless of the object's transform.
				o.vertex = float4(v.vertex.xy, 0, 1);
				return o;
			}

			void frag(v2f i, inout half4 color : SV_Target)
			{
				color.rgb = LiminalApplyColorGrade(color.rgb, _Brightness, _Contrast, _HueShift, _Saturation);
			}
			ENDCG
		}
	}
	Fallback Off
}
