Shader "Hidden/SceneTransition"
{
    Properties
    {
        _Progress ("Progress", Range(0,1)) = 0
        _Color ("Color", Color) = (0,0,0,1)
        _Pattern ("Pattern", Int) = 0
        _Aspect ("Aspect", Float) = 1.777
        _Softness ("Softness", Range(0,0.1)) = 0.02
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Progress;
            float4 _Color;
            int _Pattern;
            float _Aspect;
            float _Softness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float dist = 0;

                // Pattern 0: Circle Iris
                if (_Pattern == 0)
                {
                    float2 aspect = float2(_Aspect, 1.0);
                    dist = length((uv - center) * aspect);
                    dist = dist / (length(float2(0.5, 0.5) * aspect));
                }
                // Pattern 1: Diamond Iris
                else if (_Pattern == 1)
                {
                    float2 aspect = float2(_Aspect, 1.0);
                    float2 d = abs((uv - center) * aspect);
                    dist = (d.x + d.y);
                    dist = dist / (0.5 * _Aspect + 0.5);
                }
                // Pattern 2: Horizontal Wipe
                else if (_Pattern == 2)
                {
                    dist = uv.x;
                }
                // Pattern 3: Fade
                else
                {
                    dist = 0.5;
                }

                float threshold = _Progress;

                if (_Pattern == 3)
                {
                    float4 col = _Color;
                    col.a = _Progress;
                    return col;
                }

                float alpha = smoothstep(threshold - _Softness, threshold + _Softness, dist);
                alpha = 1.0 - alpha;

                float4 col = _Color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
