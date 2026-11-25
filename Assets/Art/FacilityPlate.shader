Shader "Polarity/Facility Plate"
{
    Properties
    {
        _Color ("Coating", Color) = (.73,.74,.75,1)
        _Glossiness ("Smoothness", Range(0,1)) = .27
        _Metallic ("Metallic", Range(0,1)) = .1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness, _Metallic;
        struct Input { float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p, float2(23.13, 85.17))) * 4375.19); }
        float noise(float2 p)
        {
            float2 b = floor(p);
            float2 f = smoothstep(0, 1, frac(p));
            return lerp(lerp(hash(b), hash(b + float2(1,0)), f.x),
                        lerp(hash(b + float2(0,1)), hash(b + 1), f.x), f.y);
        }
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            float2 p = float2(i.worldPos.x + .37 * i.worldPos.y, i.worldPos.z + i.worldPos.y);
            float n = noise(p * .74) * .045 + noise(p * 50) * .026;
            float marks = smoothstep(.85, .97, noise(p * float2(7, 84))) * .045;
            o.Albedo = _Color.rgb * (.96 + n - marks);
            o.Smoothness = _Glossiness + n * 1.5;
            o.Metallic = _Metallic;
        }
        ENDCG
    }
    FallBack "Standard"
}
