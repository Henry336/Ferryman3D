Shader "Ferryman/Water"
{
    // Stylized river: vertex waves (same formula as Waves.cs), banded colour, crest foam,
    // foam ring and wake around the boat, drifting soul glow under the surface, toon glints.
    Properties
    {
        _Deep ("Deep", Color) = (0.02,0.08,0.1,1)
        _Light ("Light", Color) = (0.12,0.42,0.45,1)
        _Foam ("Foam", Color) = (0.75,0.95,0.95,1)
        _Glow ("Soul Glow", Color) = (0.45,0.95,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Deep, _Light, _Foam, _Glow;
            float _WaveAmp, _WaveScroll, _WaveTime, _BoatHalf;
            float4 _FogColor; float _FogStart, _FogEnd;

            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 n : TEXCOORD1; float h : TEXCOORD2; };

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float a = hash(i), b = hash(i + float2(1, 0)), c = hash(i + float2(0, 1)), d = hash(i + float2(1, 1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert (appdata_base v)
            {
                v2f o;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float X = wp.x + _WaveScroll, z = wp.z, t = _WaveTime;
                float a1 = 0.55 * X + 1.3 * t, a2 = 1.1 * X + 0.4 * z - 1.9 * t, a3 = 2.3 * X - 0.7 * z + 2.6 * t;
                float h = _WaveAmp * (0.55 * sin(a1) + 0.30 * sin(a2) + 0.15 * sin(a3));
                float dx = _WaveAmp * (0.55 * 0.55 * cos(a1) + 0.30 * 1.1 * cos(a2) + 0.15 * 2.3 * cos(a3));
                float dz = _WaveAmp * (0.30 * 0.4 * cos(a2) - 0.15 * 0.7 * cos(a3));
                wp.y += h;
                o.wp = wp;
                o.h = h / max(0.001, _WaveAmp);
                o.n = normalize(float3(-dx, 1, -dz));
                o.pos = mul(UNITY_MATRIX_VP, float4(wp, 1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float3 l = normalize(_WorldSpaceLightPos0.xyz);
                float2 p = float2(i.wp.x + _WaveScroll, i.wp.z);

                // Banded body colour: brighter on crests and at grazing angles.
                float fres = pow(1 - saturate(dot(n, v)), 2.5);
                float t = saturate(i.h * 0.35 + 0.35 + fres * 0.5);
                t = floor(t * 4) / 4;
                float3 col = lerp(_Deep.rgb, _Light.rgb, t);

                // Souls drifting below the surface.
                float2 gp = p * 0.22 + float2(-_WaveTime * 0.05, _WaveTime * 0.03);
                float g = vnoise(gp) * vnoise(gp * 2.3 + 7.1);
                float soul = smoothstep(0.32, 0.5, g) * (0.6 + 0.4 * sin(_WaveTime * 1.7 + p.x * 0.3));
                col += _Glow.rgb * soul * 0.35;

                // Crest foam streaks.
                float streak = vnoise(float2(p.x * 1.6, p.y * 0.5 + _WaveTime * 0.2));
                float crest = smoothstep(0.55, 0.85, i.h) * step(0.5, streak);

                // Foam ring hugging the hull and a V-shaped wake trailing behind.
                float hb = max(0.5, _BoatHalf);
                float d = length(float2(i.wp.x / (hb + 0.35), i.wp.z / 0.95));
                float ringNoise = vnoise(p * 3 + _WaveTime * 2);
                float ring = smoothstep(0.16, 0.0, abs(d - 1) - ringNoise * 0.08);
                float behind = -(i.wp.x + hb);
                float wakeLine = abs(abs(i.wp.z) - (0.7 + behind * 0.18));
                float wake = step(0, behind) * smoothstep(0.25, 0.0, wakeLine - ringNoise * 0.12) * exp(-behind * 0.07);
                float foam = saturate(crest * 0.7 + ring + wake * 0.8);
                col = lerp(col, _Foam.rgb, foam);

                // Toon glint from the moon.
                float spec = pow(saturate(dot(reflect(-l, n), v)), 80);
                col += step(0.5, spec) * 0.45;

                float fog = saturate((distance(_WorldSpaceCameraPos, i.wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return fixed4(lerp(col, _FogColor.rgb, fog), 1);
            }
            ENDCG
        }
    }
}
