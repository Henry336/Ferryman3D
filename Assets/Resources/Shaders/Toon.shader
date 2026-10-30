Shader "Ferryman/Toon"
{
    Properties
    {
        _Color ("Lit Color", Color) = (1,1,1,1)
        _Shade ("Shade Color", Color) = (0.3,0.3,0.4,1)
        _RimColor ("Rim Color", Color) = (0.3,0.7,0.75,1)
        _RimPower ("Rim Power", Float) = 3
        _Outline ("Outline Width", Float) = 0.035
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
                float3 wn = UnityObjectToWorldNormal(v.normal);
                wp += wn * _Outline;
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                o.fog = saturate((distance(_WorldSpaceCameraPos, wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(lerp(_OutlineColor.rgb, _FogColor.rgb, i.fog), 1);
            }
            ENDCG
        }

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color, _Shade, _RimColor; float _RimPower;
            float4 _FogColor; float _FogStart, _FogEnd;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 wp : TEXCOORD1; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float ndl = dot(n, l);
                float band = smoothstep(0.0, 0.06, ndl);
                float3 col = lerp(_Shade.rgb, _Color.rgb, band);
                float rim = pow(1 - saturate(dot(n, v)), _RimPower) * smoothstep(-0.2, 0.3, ndl + 0.3);
                col += _RimColor.rgb * step(0.45, rim) * 0.5;
                float fog = saturate((distance(_WorldSpaceCameraPos, i.wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return fixed4(lerp(col, _FogColor.rgb, fog), 1);
            }
            ENDCG
        }
    }
}
