Shader "Unlit/TrailDisplayTransparent"
{
    Properties
    {
        _Trail    ("Trail", 2D) = "black" {}
        _Palette  ("Palette", 2D) = "white" {}
        _MaxValue ("Max Value", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Trail);
            SAMPLER(sampler_Trail);

            TEXTURE2D(_Palette);
            SAMPLER(sampler_Palette);

            float _MaxValue;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // Read trail intensity (lookup only)
                float v = SAMPLE_TEXTURE2D(_Trail, sampler_Trail, i.uv).r;
                float t = saturate(v / _MaxValue);

                // Palette lookup (RGBA)
                float4 palette = SAMPLE_TEXTURE2D(
                    _Palette,
                    sampler_Palette,
                    float2(t, 0.5)
                );

                // RGB from palette, alpha from palette
                return half4(palette.rgb, palette.a);
            }
            ENDHLSL
        }
    }
}
