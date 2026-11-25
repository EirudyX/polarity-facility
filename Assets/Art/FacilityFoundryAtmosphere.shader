Shader "Polarity/Foundry Atmosphere"
{
    Properties { _MainTex ("Scene", 2D) = "black" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _Mist, _Glow;
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
        float4 _MainTex_TexelSize;
        float4 _RayOrigin, _RayX, _RayY, _RayDirection;
        float2 _BlurAxis;

        float hash(float3 p)
        {
            p = frac(p * .1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }
        float noise(float3 p)
        {
            float3 v = floor(p);
            float3 f = frac(p);
            f = f * f * (3 - 2 * f);
            float a = lerp(hash(v), hash(v + float3(1,0,0)), f.x);
            float b = lerp(hash(v + float3(0,1,0)), hash(v + float3(1,1,0)), f.x);
            float c = lerp(hash(v + float3(0,0,1)), hash(v + float3(1,0,1)), f.x);
            float d = lerp(hash(v + float3(0,1,1)), hash(v + 1), f.x);
            return lerp(lerp(a,b,f.y), lerp(c,d,f.y), f.z);
        }
        float2 depthUV(float2 uv)
        {
            #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) uv.y = 1 - uv.y;
            #endif
            return uv;
        }
        half4 mist(v2f_img input) : SV_Target
        {
            float2 uv = depthUV(input.uv);
            float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
            #if defined(UNITY_REVERSED_Z)
                float barrier = lerp(_ProjectionParams.z, _ProjectionParams.y, raw);
            #else
                float barrier = lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
            #endif
            float2 screen = uv * 2 - 1;
            float3 eye = _RayOrigin.xyz + _RayX.xyz * screen.x + _RayY.xyz * screen.y;
            float3 ray = _RayDirection.xyz;
            float start = max(0, (-.95 - eye.y) / ray.y);
            float end = min(min(barrier, 76), (-31 - eye.y) / ray.y);
            if (start >= end) return 0;

            float stepSize = (end - start) / 22;
            float offset = hash(float3(input.uv * 819, .51));
            float3 drift = float3(_Time.y * .075, -.06 * _Time.y, -.038 * _Time.y);
            float transmission = 1;
            float3 light = 0;
            [loop]
            for (int sampleIndex = 0; sampleIndex < 22; sampleIndex++)
            {
                float3 p = eye + ray * (start + (sampleIndex + offset) * stepSize);
                float3 u = p * float3(.22, .58, .24) + drift;
                float curl = noise(u * .73 + float3(5,8,0));
                float n = noise(u + curl * 1.25) * .72 + noise(u * 2.42 + curl) * .28;
                float wisps = smoothstep(.36, .78, n);
                float depth = -p.y;
                float layer = smoothstep(1, 5.5, depth) * (1 - smoothstep(21, 33, depth));
                float sheet = exp(-abs(p.y + 9.6 + 3.1 * curl + sin(p.x * .31 + p.z * .19) * 1.2) * .49);
                float lowerSheet = exp(-abs(p.y + 16.9 - 3.4 * curl) * .29);
                float2 slant = float2((depth - 10) * .22 + curl * 1.6, -.10 * depth);
                float2 ventA = (p.xz - float2(-3.0, 4.2) + slant) * float2(.23,.18);
                float2 ventB = (p.xz - float2(8, 12) + slant) * float2(.19,.18);
                float2 ventC = (p.xz - float2(-15, 7) + slant) * .19;
                float vents = exp(-dot(ventA, ventA)) + .78 * exp(-dot(ventB, ventB)) + .65 * exp(-dot(ventC, ventC));
                float ribbonHeight = -6.1 + sin(p.x * .44 + p.z * .22 + _Time.y * .22) * 1.37 + (curl - .5) * 2.9;
                float ribbon = exp(-abs(p.y - ribbonHeight) * .68)
                    * smoothstep(-4.0, .5, p.z) * (1 - smoothstep(13, 21, p.z));
                float glowBillow = ribbon * wisps * (.35 + .65 * saturate(vents));
                float charge = saturate(vents * (.46 + .54 * sheet) + sheet * .32 + 1.7 * glowBillow);
                float density = layer * (.004 + wisps * (.031 * lowerSheet + .092 * sheet + vents * .081) + .32 * glowBillow);
                float opacity = 1 - exp(-density * stepSize);
                float3 tint = lerp(float3(.018,.084,.32), float3(.10,.59,1.08), saturate(charge * (.62 + .52 * wisps)));
                light += transmission * tint * opacity;
                transmission *= 1 - opacity;
            }
            return half4(light, 1 - transmission);
        }
        half4 highlights(v2f_img input) : SV_Target
        {
            half3 color = tex2D(_MainTex, input.uv).rgb;
            half high = max(color.r, max(color.g, color.b));
            half low = min(color.r, min(color.g, color.b));
            half neon = smoothstep(.28,.66,high-low) * smoothstep(.56,.98,high);
            return half4(color * neon, 1);
        }
        half4 soften(v2f_img input) : SV_Target
        {
            float2 d = _MainTex_TexelSize.xy * _BlurAxis;
            half3 result = tex2D(_MainTex, input.uv).rgb * .2270;
            result += tex2D(_MainTex, input.uv + d * 1.385).rgb * .3162;
            result += tex2D(_MainTex, input.uv - d * 1.385).rgb * .3162;
            result += tex2D(_MainTex, input.uv + d * 3.231).rgb * .0703;
            result += tex2D(_MainTex, input.uv - d * 3.231).rgb * .0703;
            return half4(result, 1);
        }
        half4 finish(v2f_img input) : SV_Target
        {
            half3 scene = tex2D(_MainTex, input.uv).rgb;
            half4 fog = tex2D(_Mist, input.uv);
            half3 glow = tex2D(_Glow, input.uv).rgb;
            return half4(scene * (1 - fog.a) + fog.rgb + glow * .60, 1);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment mist
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment highlights
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment soften
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment finish
            ENDCG
        }
    }
}
