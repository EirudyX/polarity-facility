Shader "Polarity/Facility Signal"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Ring ("Ring mode", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _Ring;
            struct Vertex { float4 pos : POSITION; float2 uv : TEXCOORD0; fixed4 col : COLOR; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; fixed4 col : COLOR; };
            Varyings vert(Vertex v)
            {
                Varyings o;
                o.clip = UnityObjectToClipPos(v.pos);
                o.uv = v.uv;
                o.col = v.col;
                return o;
            }
            fixed4 frag(Varyings i) : SV_Target
            {
                float d = lerp(abs(i.uv.y * 2 - 1), abs(length(i.uv * 2 - 1) - .77) * 6, _Ring);
                float core = saturate(1 - d * 3);
                float glow = saturate(1 - d);
                fixed4 col = lerp(i.col, 1, _Ring) * _Color;
                return fixed4(lerp(col.rgb, 1, core * .34), col.a * (core * .72 + glow * glow * .28));
            }
            ENDCG
        }
    }
}
