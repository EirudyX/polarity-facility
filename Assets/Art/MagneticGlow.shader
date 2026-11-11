Shader "Polarity/Magnetic Glow"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 position : POSITION; fixed4 color : COLOR; };
            struct output { float4 position : SV_POSITION; fixed4 color : COLOR; };
            output vert(input v)
            {
                output o;
                o.position = UnityObjectToClipPos(v.position);
                o.color = v.color;
                return o;
            }
            fixed4 frag(output i) : SV_Target { return i.color; }
            ENDCG
        }
    }
}