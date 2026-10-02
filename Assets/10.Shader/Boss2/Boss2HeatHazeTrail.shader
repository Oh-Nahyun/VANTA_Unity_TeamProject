// 투사체 트레일용 아지랑이 셰이더 (URP, 2D Renderer 에서 SRPDefaultUnlit 으로 그려짐)
//  - 노이즈 텍스처를 흐르게(스크롤) 하고, 저주파 노이즈로 UV 를 휘어 일렁이게 한다.
//  - 트레일 위아래 가장자리와 꼬리 쪽으로 갈수록 불규칙하게 잘려 나가며 옅어진다.
//  - 색은 TrailRenderer 의 Color 그라데이션 × Tint. Src/Dst Blend 로 알파·가산 둘 다 쓴다.
Shader "Boss2/HeatHazeTrail"
{
    Properties
    {
        _NoiseTex ("Noise (R: 모양, G: 일렁임)", 2D) = "gray" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 1
        _ScrollSpeed ("Scroll Speed (XY)", Vector) = (-1.5, 0.4, 0, 0)
        _NoiseScale ("Noise Scale (XY)", Vector) = (2, 1, 0, 0)
        _Distortion ("Distortion (일렁임 세기)", Range(0, 0.5)) = 0.15
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.4
        _Cutoff ("Noise Cutoff (클수록 듬성듬성)", Range(0, 1)) = 0.35
        _TailFade ("Tail Fade (꼬리 쪽 소멸)", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend (알파 10, 가산 1)", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTex_ST;
                half4 _Tint;
                float _Intensity;
                float4 _ScrollSpeed;
                float4 _NoiseScale;
                float _Distortion;
                float _EdgeSoftness;
                float _Cutoff;
                float _TailFade;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float2 uv = input.uv * _NoiseScale.xy;

                // 1. 저주파 노이즈(G)로 UV 를 휘게 해서 아지랑이처럼 일렁이게 한다.
                float2 warpUV = uv * 0.5 + time * _ScrollSpeed.xy * 0.5;
                float2 warp = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, warpUV).gr - 0.5;

                // 2. 휘어진 UV 로 흐르는 노이즈(R)를 읽어 불꽃 모양을 만든다.
                float2 flameUV = uv + warp * _Distortion * 4.0 + time * _ScrollSpeed.xy;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flameUV).r;

                // 3. 트레일 위아래 가장자리, 꼬리 쪽(uv.x → 1)으로 갈수록 노이즈에 잘려 나간다.
                float edge = smoothstep(0.0, _EdgeSoftness, 1.0 - abs(input.uv.y * 2.0 - 1.0));
                float tail = 1.0 - input.uv.x * _TailFade;
                float shape = noise * edge * tail + edge * tail * 0.35;
                float mask = smoothstep(_Cutoff, _Cutoff + 0.25, shape);

                half4 color = input.color * _Tint;
                color.rgb *= _Intensity;
                color.a *= mask;
                return color;
            }
            ENDHLSL
        }
    }
}
