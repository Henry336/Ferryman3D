Shader "Ferryman/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Core ("Core", Float) = 0.35
        _Power ("Fresnel Power", Float) = 2
        _Radial ("Radial (quads)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Core, _Power, _Radial;
            float _FogStart, _FogEnd;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 wp : TEXCOORD1; float2 uv : TEXCOORD2; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.texcoord.xy;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float fres = pow(1 - abs(dot(normalize(i.n), v)), _Power);
                float a = saturate(_Core + fres);
                float r = saturate(1 - length(i.uv - 0.5) * 2);
                a = lerp(a, r * r, step(0.5, _Radial));
                float fog = saturate((distance(_WorldSpaceCameraPos, i.wp) - _FogStart) / max(0.01, _FogEnd - _FogStart));
                return fixed4(_Color.rgb * a * _Color.a * (1 - fog * 0.85), 1);
            }
            ENDCG
        }
    }
}
