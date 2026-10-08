Shader "UI/RadialFade"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Progress ("Progress", Range(0,1)) = 0
        _Softness ("Softness", Range(0.001, 0.5)) = 0.15
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Progress;
            float _Softness;
            float4 _Center;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - _Center.xy;
                float dist = length(centeredUV) * 2.0;
                float threshold = lerp(1.5, -_Softness, _Progress); // 페이드 경계가 바깥쪽에서 중앙으로 이동
                float alpha = smoothstep(threshold - _Softness, threshold + _Softness, dist);

                if (_Progress <= 0.001)
                {
                    alpha = 0.0; // 시작은 완전 투명
                }

                if (_Progress >= 0.999)
                {
                    alpha = 1.0; // 끝은 완전 검정
                }

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}