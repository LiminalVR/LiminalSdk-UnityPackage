Shader "Liminal/PostFX/ColorGrading"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Brightness("Brightness", Range(0, 2)) = 1
		_Contrast("Contrast", Range(0, 2)) = 1
		_HueShift("Hue Shift (Degrees)", Range(-180, 180)) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }

		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			// Screenspace texture macros resolve to a Texture2DArray under
			// Single Pass Instanced (Vulkan) and Multiview (GLES3) on Quest.
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			float4 _MainTex_ST;
			half _Brightness;
			half _Contrast;
			half _HueShift;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			// float (not half) here — half precision causes hue banding on mobile.
			float3 RGBToHSV(float3 c)
			{
				float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
				float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
				float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
				float d = q.x - min(q.w, q.y);
				float e = 1.0e-4;
				return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
			}

			float3 HSVToRGB(float3 c)
			{
				float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
				float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
				return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
			}

			half4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				half4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST));

				float3 hsv = RGBToHSV(col.rgb);
				hsv.x = frac(hsv.x + _HueShift * (1.0 / 360.0) + 1.0);
				col.rgb = HSVToRGB(hsv);

				col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;
				col.rgb *= _Brightness;

				return half4(saturate(col.rgb), col.a);
			}
			ENDCG
		}
	}
	Fallback Off
}
