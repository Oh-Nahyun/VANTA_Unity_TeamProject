// 스프라이트 모양 그대로 빛을 더한다(가산). 원래 색과 _GlowColor 를 _ColorMix 비율로 섞고, HDR 색이면 Bloom 으로 번진다. (URP, 조명 무관)
// 광역기 폭발의 검흔 위에 겹쳐 검흔이 빛나 보이게 할 때 쓴다. SpriteRenderer.color 의 알파로 빛의 세기를 조절한다.
Shader "Boss2/SpriteAdditiveGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (3, 1.2, 2.4, 1)
        _ColorMix ("Sprite Color → Glow Color", Range(0, 1)) = 0.6
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

        Blend SrcAlpha One
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
                half4 _GlowColor;
                half _ColorMix;
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
                // 원래 색의 밝기를 유지한 채 빛 색으로 물들이고, HDR 세기만큼 밝힌다.
                half intensity = max(max(_GlowColor.r, _GlowColor.g), max(_GlowColor.b, 0.0001));
                half3 tinted = lerp(tex.rgb, _GlowColor.rgb / intensity, _ColorMix);
                half4 color;
                color.rgb = tinted * intensity * input.color.rgb;
                color.a = tex.a * input.color.a * _GlowColor.a;
                return color;
            }
            ENDHLSL
        }
    }
}
