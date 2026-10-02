// 스프라이트를 밝기(흑백)로 바꾼 뒤 지정한 색으로 다시 칠한다. (URP, 조명 무관)
// 주황 불덩이 스프라이트를 보라·검붉은색 발사체로 재사용할 때 쓴다. HDR 색이면 Bloom 으로 빛난다.
Shader "Boss2/SpriteRecolor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Contrast ("Contrast", Range(0.5, 3)) = 1.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            // URP 2D 공용 함수(UnityFlipSprite, SetUpSpriteInstanceProperties)를 가져다 쓰기만 한다. 패키지 파일은 바꾸지 않는다.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                float _Contrast;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // 기본 스프라이트 셰이더와 같은 방식으로 SpriteRenderer 의 flipX/flipY 와 Color 를 반영한다.
                SetUpSpriteInstanceProperties();
                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.color = input.color * unity_SpriteColor;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half luminance = dot(tex.rgb, half3(0.299, 0.587, 0.114));
                luminance = saturate(pow(luminance, _Contrast));

                half4 color;
                color.rgb = luminance * _Tint.rgb * input.color.rgb;
                color.a = tex.a * input.color.a * _Tint.a;
                return color;
            }
            ENDHLSL
        }
    }
}
