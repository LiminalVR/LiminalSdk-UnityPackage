#ifndef LIMINAL_COLOR_GRADE_INCLUDED
#define LIMINAL_COLOR_GRADE_INCLUDED

// Shared by ColorGrading.shader (blit path, editor/PC) and
// ColorGradingFetch.shader (framebuffer-fetch path, Quest) so the two
// paths grade identically.

// float (not half) here — half precision causes hue banding on mobile.
float3 LiminalRGBToHSV(float3 c)
{
	float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
	float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-4;
	return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 LiminalHSVToRGB(float3 c)
{
	float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

half3 LiminalApplyColorGrade(half3 rgb, half brightness, half contrast, half hueShift, half saturation)
{
	float3 hsv = LiminalRGBToHSV(rgb);
	hsv.x = frac(hsv.x + hueShift * (1.0 / 360.0) + 1.0);
	rgb = LiminalHSVToRGB(hsv);

	// Luma-weighted saturation (matches standard grading, unlike
	// scaling HSV.y which greys toward the max channel instead).
	half lum = dot(rgb, half3(0.2126, 0.7152, 0.0722));
	rgb = lerp(half3(lum, lum, lum), rgb, saturation);

	rgb = (rgb - 0.5) * contrast + 0.5;
	rgb *= brightness;

	return saturate(rgb);
}

#endif // LIMINAL_COLOR_GRADE_INCLUDED
