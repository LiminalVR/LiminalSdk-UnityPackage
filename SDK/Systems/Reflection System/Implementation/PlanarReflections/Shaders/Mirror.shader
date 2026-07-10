Shader "FX/MirrorReflection"
{
    Properties
    {
        // Left-eye (or mono) reflection. Kept as _ReflectionTex so existing materials/scripts keep working.
        [HideInInspector] _ReflectionTex("", 2D) = "white" {}
        [HideInInspector] _ReflectionTexRight("", 2D) = "white" {}

        _RampTex("Ramp", 2D) = "white" {}
        _Color("Color", Color) = (0.5, 0.5, 0.5, 1)
        _ColorHorizon("Horizon Color", Color) = (0.5, 0.5, 0.5, 1)
        [Normal] _RippleTex("Ripple", 2D) = "white" {}
        _RippleStrength("Ripple Strength", Float) = 0.5
        _RippleSpeed("Ripple Speed", Float) = 0
        _ReflectionStrength("Reflection Strength", Float) = 0.5
        _FadeDistance("Fade Distance", Float) = 0
        _FadeScaleX("Fade Scale X", Float) = 4

         MySrcMode("SrcMode", Float) = 1
         MyDstMode("DstMode", Float) = 1

        [MaterialToggle] _EnableTint("_EnableTint", Float) = 0
        [MaterialToggle] _EnableRampAlpha("_EnableRampAlpha", Float) = 1
    }
        SubShader
        {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend[MySrcMode][MyDstMode]
        Cull Off
        ZWrite Off

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 refl : TEXCOORD1;
                    float2 uvRipple : TEXCOORD2;
                    float4 screenPos : TEXCOORD3;
                    float4 worldPos : TEXCOORD4;
                    float4 pos : SV_POSITION;
                    float2 uvRamp : TEXCOORD5;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                // The reflection is rendered per eye with that eye's actual view/projection
                // matrices, so sampling at the fragment's own screen position is exact on
                // any headset/IPD - no per-device correction needed.
                sampler2D _ReflectionTex;      // left eye, or mono when stereo is off
                sampler2D _ReflectionTexRight;

                sampler2D _RampTex;
                float4 _RampTex_ST;

                sampler2D _RippleTex;
                float4 _RippleTex_ST;

                fixed4 _Color;
                fixed4 _ColorHorizon;

                fixed _RippleSpeed;
                fixed _RippleStrength;
                fixed _ReflectionStrength;
                fixed _FadeDistance;
                fixed _FadeScaleX;

                float _CustomTime; // (relies on a script running on the CPU - will not work without it)

                float _EnableTint;
                float _EnableRampAlpha;

                struct appdata
                {
                    float4 pos : POSITION;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                v2f vert(appdata v)
                {
                    float4 pos = v.pos;
                    float2 uv = v.uv;

                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = UnityObjectToClipPos(pos);
                    o.uv = TRANSFORM_TEX(uv, _MainTex);
                    o.uvRipple = TRANSFORM_TEX(uv, _RippleTex);
                    o.refl = ComputeScreenPos(o.pos);
                    o.uvRamp = TRANSFORM_TEX(uv, _RampTex);

                    o.screenPos = ComputeNonStereoScreenPos(o.pos);
                    o.worldPos = mul(unity_ObjectToWorld, pos);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    fixed4 main = _Color;
                    fixed4 ramp = tex2D(_RampTex, i.uvRamp);

                    // Ripple
                    float offset = _CustomTime != 0 ? _CustomTime : _Time.x * _RippleSpeed;
                    float2 uvr = i.uvRipple + float2(0, offset);
                    fixed3 nrm = UnpackNormal(tex2D(_RippleTex, uvr));
                    i.screenPos.xy += nrm.r * _RippleStrength;

                    float2 uvRefl = i.screenPos.xy / i.screenPos.w;

                    fixed4 refl;
                    if (unity_StereoEyeIndex == 0)
                        refl = tex2D(_ReflectionTex, uvRefl);
                    else
                        refl = tex2D(_ReflectionTexRight, uvRefl);
                    refl *= _ReflectionStrength;

                    // Fade reflection over distance
                    fixed2 delta = _WorldSpaceCameraPos.xz - i.worldPos.xz;
                    fixed dist = length(fixed2(delta.x * _FadeScaleX, delta.y));

                    //refl *= saturate(pow(dist / _FadeDistance, 4));

                    refl = saturate(refl - Luminance(main) / 6);

                    fixed4 col = saturate(main + refl);

                    // Apply ramp to fade toward horizon color
                    col = lerp(col, _ColorHorizon, 1 - ramp);

                    if(_EnableTint)
                        col *= _Color;

                    if(_EnableRampAlpha)
                        col.a = lerp(1,0, 1 - ramp);

                    return col;
                }
                ENDCG
            }
        }
}
