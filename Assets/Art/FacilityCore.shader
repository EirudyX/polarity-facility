Shader "Polarity/Luminous Core"
{
    Properties
    {
        _Color ("Polarity", Color) = (1,.13,.22,1)
        _Glossiness ("Shell gloss", Range(0,1)) = .73
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard addshadow
        #pragma target 3.0
        fixed4 _Color;
        half _Glossiness;
        struct Input { float3 viewDir; };
        void surf(Input i, inout SurfaceOutputStandard o)
        {
            half face = saturate(dot(normalize(i.viewDir), o.Normal));
            half rim = pow(1 - face, 2.0);
            half shoulder = 1 - pow(abs(.57 - face), 1.4);
            o.Albedo = _Color.rgb * .24;
            o.Smoothness = _Glossiness;
            o.Emission = _Color.rgb * (.48 + .22 * shoulder + .95 * rim)
                + rim * rim * .16 + pow(face, 10) * .055;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
