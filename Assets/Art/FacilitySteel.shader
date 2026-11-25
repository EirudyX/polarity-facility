Shader "Polarity/Weathered Steel"
{
    Properties
    {
        _Color ("Oxidized coating", Color) = (.13,.19,.27,1)
        _Metallic ("Metallic", Range(0,1)) = .52
        _Glossiness ("Glossiness", Range(0,1)) = .42
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard addshadow
        #pragma target 3.0
        fixed4 _Color;
        half _Metallic, _Glossiness;
        struct Input { float3 worldPos; };
        float hash(float2 p)
        {
            p = frac(p * float2(.1031, .11369));
            p += dot(p, p.yx + 19.19);
            return frac((p.x + p.y) * p.x);
        }
        float grain(float2 p)
        {
            float2 j = floor(p);
            float2 a = frac(p);
            a = a * a * (3 - 2 * a);
            return lerp(lerp(hash(j), hash(j + float2(1,0)), a.x),
                        lerp(hash(j + float2(0,1)), hash(j + 1), a.x), a.y);
        }
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float2 p = float2(i.worldPos.x + .73 * i.worldPos.z, i.worldPos.y + .19 * i.worldPos.z);
            float patina = grain(p * float2(1.7, .72));
            float runoff = grain(p * float2(17, .52)) * grain(p * float2(4.1, .61));
            float scars = smoothstep(.81, .97, grain(p * float2(115, 19))) * (.35 + runoff);
            float abrasion = saturate(patina * .34 + runoff * .22 + scars * .19);
            o.Albedo = _Color.rgb * (.80 + abrasion) + scars * float3(.05, .046, .035);
            o.Metallic = _Metallic * (.78 + patina * .32);
            o.Smoothness = saturate(_Glossiness - runoff * .22 + scars * .16);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
