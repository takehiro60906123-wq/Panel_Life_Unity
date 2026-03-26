Shader "UI/LiquidBoard"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Base Colors)]
        _ColorDeep ("Deep Color", Color) = (0.04, 0.08, 0.12, 0.85)
        _ColorMid ("Mid Color", Color) = (0.06, 0.18, 0.16, 0.75)
        _ColorSurface ("Surface Color", Color) = (0.12, 0.28, 0.22, 0.65)
        _ColorHighlight ("Highlight Color", Color) = (0.25, 0.55, 0.40, 0.30)

        [Header(Wave Motion)]
        _WaveSpeed ("Wave Speed", Range(0.05, 2.0)) = 0.35
        _WaveScale ("Wave Scale", Range(1, 20)) = 6.0
        _WaveAmplitude ("Wave Amplitude", Range(0.001, 0.05)) = 0.012
        _WaveSecondary ("Secondary Wave Scale", Range(1, 30)) = 12.0
        _WaveSecondaryAmp ("Secondary Wave Amp", Range(0.001, 0.03)) = 0.006

        [Header(Surface Detail)]
        _NoiseScale ("Noise Scale", Range(1, 30)) = 10.0
        _NoiseSpeed ("Noise Speed", Range(0.01, 1.0)) = 0.15
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.18
        _CausticScale ("Caustic Scale", Range(1, 20)) = 8.0
        _CausticSpeed ("Caustic Speed", Range(0.05, 1.0)) = 0.2
        _CausticStrength ("Caustic Strength", Range(0, 0.4)) = 0.12

        [Header(Edge)]
        _EdgeGlow ("Edge Glow Width", Range(0, 0.15)) = 0.06
        _EdgeGlowColor ("Edge Glow Color", Color) = (0.15, 0.4, 0.3, 0.4)
        _CornerRadius ("Corner Radius", Range(0, 0.15)) = 0.04

        [Header(Ripple)]
        _RippleColor ("Ripple Color", Color) = (0.3, 0.7, 0.55, 0.5)
        _RippleSpeed ("Ripple Speed", Range(0.5, 5.0)) = 2.5
        _RippleWidth ("Ripple Ring Width", Range(0.01, 0.15)) = 0.045
        _RippleDuration ("Ripple Duration", Range(0.2, 2.0)) = 0.8
        _RippleDistortion ("Ripple Distortion", Range(0.0, 0.06)) = 0.025

        [Header(Stencil)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            float4 _ColorDeep;
            float4 _ColorMid;
            float4 _ColorSurface;
            float4 _ColorHighlight;

            float _WaveSpeed;
            float _WaveScale;
            float _WaveAmplitude;
            float _WaveSecondary;
            float _WaveSecondaryAmp;

            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseStrength;
            float _CausticScale;
            float _CausticSpeed;
            float _CausticStrength;

            float _EdgeGlow;
            float4 _EdgeGlowColor;
            float _CornerRadius;

            float4 _RippleColor;
            float _RippleSpeed;
            float _RippleWidth;
            float _RippleDuration;
            float _RippleDistortion;

            // 波紋データ: xy=中心UV, z=開始時刻, w=強度（0なら無効）
            float4 _Ripple0;
            float4 _Ripple1;
            float4 _Ripple2;
            float4 _Ripple3;

            float4 _ClipRect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            // ── ノイズ関数 ──
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise2d(float2 p)
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
                float2 shift = float2(100, 100);

                for (int i = 0; i < 3; i++)
                {
                    v += a * noise2d(p);
                    p = p * 2.0 + shift;
                    a *= 0.5;
                }

                return v;
            }

            // ── コースティクス風パターン ──
            float caustic(float2 uv, float time)
            {
                float2 p = uv * _CausticScale;
                float v1 = sin(p.x * 1.3 + time * 0.7) * cos(p.y * 1.7 + time * 0.5);
                float v2 = sin(p.x * 2.1 - time * 0.4) * cos(p.y * 1.1 + time * 0.8);
                float v3 = sin((p.x + p.y) * 1.5 + time * 0.6);
                return (v1 + v2 + v3) * 0.33 * 0.5 + 0.5;
            }

            // ── 角丸マスク ──
            float roundedBox(float2 uv, float radius)
            {
                float2 d = abs(uv - 0.5) - (0.5 - radius);
                float dist = length(max(d, 0.0)) - radius;
                return smoothstep(0.005, -0.005, dist);
            }

            // ── 単一波紋の計算 ──
            // rippleData: xy=中心UV, z=開始時刻, w=強度
            // 戻り値: x=UV歪み量, y=可視輝度
            float2 calcRipple(float2 uv, float4 rippleData, float time)
            {
                if (rippleData.w <= 0.001) return float2(0, 0);

                float elapsed = time - rippleData.z;
                if (elapsed < 0.0 || elapsed > _RippleDuration) return float2(0, 0);

                float progress = elapsed / _RippleDuration;
                float radius = progress * _RippleSpeed;
                float dist = length(uv - rippleData.xy);

                // リング状の波紋
                float ring = 1.0 - saturate(abs(dist - radius) / _RippleWidth);
                ring = ring * ring; // シャープに

                // フェードアウト
                float fade = 1.0 - progress;
                fade = fade * fade;

                float strength = ring * fade * rippleData.w;

                // UV歪み（波紋の中心から外へ押し出す方向）
                float2 dir = (uv - rippleData.xy);
                float dirLen = length(dir);
                dir = dirLen > 0.001 ? dir / dirLen : float2(0, 0);
                float distortAmount = strength * _RippleDistortion;

                return float2(distortAmount, strength);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = v.vertex;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                // ── UV 歪み（液状の揺らぎ）──
                float2 waveUV = uv * _WaveScale;
                float wave1 = sin(waveUV.x + time * _WaveSpeed * 1.3) *
                              cos(waveUV.y + time * _WaveSpeed * 0.9);
                float wave2 = sin(waveUV.y * 0.7 - time * _WaveSpeed * 1.1) *
                              cos(waveUV.x * 1.2 + time * _WaveSpeed * 0.7);

                float2 distortion = float2(wave1, wave2) * _WaveAmplitude;

                // 二次波（細かい揺らぎ）
                float2 wave2UV = uv * _WaveSecondary;
                float w3 = sin(wave2UV.x * 1.5 + time * _WaveSpeed * 2.1);
                float w4 = cos(wave2UV.y * 1.8 - time * _WaveSpeed * 1.7);
                distortion += float2(w3, w4) * _WaveSecondaryAmp;

                // ── 波紋による追加歪み ──
                float totalRippleBright = 0;
                float2 rippleDistort = float2(0, 0);

                float2 r0 = calcRipple(uv, _Ripple0, time);
                float2 r1 = calcRipple(uv, _Ripple1, time);
                float2 r2 = calcRipple(uv, _Ripple2, time);
                float2 r3 = calcRipple(uv, _Ripple3, time);

                totalRippleBright = r0.y + r1.y + r2.y + r3.y;

                // 波紋歪み: 各波紋の中心から外向きに押し出す
                float2 d0 = uv - _Ripple0.xy; float l0 = length(d0);
                rippleDistort += (l0 > 0.001 ? (d0 / l0) : float2(0,0)) * r0.x;
                float2 d1 = uv - _Ripple1.xy; float l1 = length(d1);
                rippleDistort += (l1 > 0.001 ? (d1 / l1) : float2(0,0)) * r1.x;
                float2 d2 = uv - _Ripple2.xy; float l2 = length(d2);
                rippleDistort += (l2 > 0.001 ? (d2 / l2) : float2(0,0)) * r2.x;
                float2 d3 = uv - _Ripple3.xy; float l3 = length(d3);
                rippleDistort += (l3 > 0.001 ? (d3 / l3) : float2(0,0)) * r3.x;

                distortion += rippleDistort;

                float2 distortedUV = uv + distortion;

                // ── ノイズ層（深さと濁り）──
                float n = fbm(distortedUV * _NoiseScale + time * _NoiseSpeed);

                // ── 色の混合 ──
                // ノイズ値で深い色～表面色をブレンド
                float depthFactor = n * _NoiseStrength + 0.5;
                float4 baseColor = lerp(_ColorDeep, _ColorMid, saturate(depthFactor));
                baseColor = lerp(baseColor, _ColorSurface, saturate(depthFactor * depthFactor));

                // ── コースティクス（水底の光模様）──
                float c = caustic(distortedUV, time * _CausticSpeed);
                float causticMask = smoothstep(0.45, 0.75, c);
                baseColor = lerp(baseColor, _ColorHighlight, causticMask * _CausticStrength);

                // ── 波紋の光 ──
                float rippleVis = saturate(totalRippleBright);
                baseColor = lerp(baseColor, _RippleColor, rippleVis * 0.7);
                baseColor.a = max(baseColor.a, rippleVis * _RippleColor.a);

                // ── 端の光（盤面の境界を示す）──
                float edgeDist = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                float edgeMask = 1.0 - smoothstep(0.0, _EdgeGlow, edgeDist);
                // 端を少し脈動させる
                float edgePulse = sin(time * 1.5 + uv.x * 3.0 + uv.y * 2.0) * 0.3 + 0.7;
                baseColor = lerp(baseColor, _EdgeGlowColor, edgeMask * edgePulse);

                // ── 角丸 ──
                float mask = roundedBox(uv, _CornerRadius);
                baseColor.a *= mask;

                // ── 頂点カラー（UI の tint）──
                baseColor *= i.color;

                // ── UI クリッピング ──
                baseColor.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

                return baseColor;
            }
            ENDCG
        }
    }
}
