Shader "Hidden/URP/SpriteOutlineURP"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness (texels)", Range(0,8)) = 2
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteOutline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // x=1/width, y=1/height
            float4 _Color;
            float4 _OutlineColor;
            float  _OutlineThickness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Sample alpha at a given uv
            inline float AlphaAt(float2 uv)
            {
                float4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return s.a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                float a = main.a;

                // Early out: if interior pixel, draw sprite normally
                if (a > 0.0)
                {
                    return main;
                }

                // Else we’re in transparent texel; check neighbors for outline
                // Thickness is in texels; convert to UV offsets using _MainTex_TexelSize
                float t = _OutlineThickness;
                float2 texel = _MainTex_TexelSize.xy;

                // Sample a small cross + diagonals; you can add more taps for thicker/rounder outlines
                float outlineAlpha = 0.0;
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2( texel.x * t, 0)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2(-texel.x * t, 0)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2(0,  texel.y * t)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2(0, -texel.y * t)));

                // Diagonals help corners look nicer
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2( texel.x * t,  texel.y * t)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2(-texel.x * t,  texel.y * t)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2( texel.x * t, -texel.y * t)));
                outlineAlpha = max(outlineAlpha, AlphaAt(IN.uv + float2(-texel.x * t, -texel.y * t)));

                if (outlineAlpha > 0.0)
                {
                    // Solid outline; if you prefer soft edges, multiply by outlineAlpha
                    return float4(_OutlineColor.rgb, _OutlineColor.a);
                }

                // Fully transparent
                return 0;
            }
            ENDHLSL
        }
    }
}
