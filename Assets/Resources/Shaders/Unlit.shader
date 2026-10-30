Shader "Ferryman/Unlit"
{
    Properties
    {
        _Top ("Top", Color) = (0.1,0.1,0.2,1)
        _Bottom ("Bottom", Color) = (0.2,0.3,0.35,1)
        _Y0 ("Bottom Y (world)", Float) = -4
        _Y1 ("Top Y (world)", Float) = 18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Top, _Bottom; float _Y0, _Y1;
            struct v2f { float4 pos : SV_POSITION; float y : TEXCOORD0; };
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.y = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate((i.y - _Y0) / max(0.01, _Y1 - _Y0));
                return fixed4(lerp(_Bottom.rgb, _Top.rgb, t), 1);
            }
            ENDCG
        }
    }
}
