Shader "Custom/UIGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorTop    ("Color Top",    Color) = (1, 1, 1, 1)
        _ColorBottom ("Color Bottom", Color) = (0, 0, 0, 1)
        _Angle       ("Angle (degrees)", Range(0, 360)) = 0
        _Smoothness  ("Smoothness",   Range(0.01, 1)) = 1
        _Offset      ("Offset",       Range(-1, 1)) = 0

        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID",         Float) = 0
        _StencilOp      ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",          Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float4 color     : COLOR;
                float2 uv        : TEXCOORD0;
                float4 worldPos  : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _ColorTop;
            float4    _ColorBottom;
            float     _Angle;
            float     _Smoothness;
            float     _Offset;
            float4    _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex   = TransformObjectToHClip(v.vertex.xyz);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.color    = v.color;
                o.worldPos = v.vertex;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float rad = _Angle * 3.14159265 / 180.0;

                float2 dir = float2(cos(rad), sin(rad));

                float2 centeredUV = i.uv - 0.5;
                float t = dot(centeredUV, dir) + 0.5;

                t = t + _Offset;

                t = saturate((t - (1.0 - _Smoothness) * 0.5) / _Smoothness);

                float4 gradientColor = lerp(_ColorBottom, _ColorTop, t);

                float4 texColor = tex2D(_MainTex, i.uv);
                float4 col = gradientColor * texColor * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                float2 inside = step(_ClipRect.xy, i.worldPos.xy) * step(i.worldPos.xy, _ClipRect.zw);
                col.a *= inside.x * inside.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}

