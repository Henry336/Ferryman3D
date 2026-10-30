Shader "Ferryman/ToonTex"
{
    // Toon for painted Meshy models: the texture already carries the 2D colouring, so lighting is one hard band plus an ink outline.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShadeMul ("Shade Multiplier", Color) = (0.62,0.62,0.75,1)
        _RimColor ("Rim Color", Color) = (0.3,0.7,0.75,1)
        _RimPower ("Rim Power", Float) = 3
        _Emission ("Emission", Float) = 0
        _Outline ("Outline Width", Float) = 0.02
        _OutlineColor ("Outline Color", Color) = (0.02,0.02,0.03,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Outline; fixed4 _OutlineColor;
            float4 _FogColor; float _FogStart, _FogEnd;
            struct v2f { float4 pos : SV_POSITION; float fog : TEXCOORD0; };
            v2f vert (appdata_base v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                wp += UnityObjectToWorldNormal(v.normal) * _Outline;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                o.fog = saturate((distance(_WorldSpaceCameraPos, wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target { return fixed4(lerp(_OutlineColor.rgb, _FogColor.rgb, i.fog), 1); }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _ShadeMul, _RimColor; float _RimPower, _Emission;
            float4 _FogColor; float _FogStart, _FogEnd;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 wp : TEXCOORD2; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 base = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                float3 n = normalize(i.n);
                float ndl = dot(n, normalize(_WorldSpaceLightPos0.xyz));
                float3 col = lerp(base * _ShadeMul.rgb, base, smoothstep(-0.05, 0.05, ndl));
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float rim = pow(1 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * step(0.5, rim) * 0.35 + base * _Emission;
                float fog = saturate((distance(_WorldSpaceCameraPos, i.wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return fixed4(lerp(col, _FogColor.rgb, fog), 1);
            }
            ENDCG
        }
    }
}
