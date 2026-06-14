Shader "TessellationEffects/TessellationRipple"
{
    Properties
    {
        _Color ("Color", Color) = (0.35, 0.55, 0.85, 1)
        _Tessellation ("Tessellation Factor", Range(1, 64)) = 16
        _RippleHeight ("Ripple Height", Float) = 0.08
        _RipplePulseCount ("Ripple Pulse Count", Float) = 0
        _RippleAxis ("Ripple Axis", Float) = 1
        _RippleDirectionSign ("Ripple Direction Sign", Float) = 1
        _RippleAxisMin ("Ripple Axis Min", Float) = -1
        _RippleAxisMax ("Ripple Axis Max", Float) = 1
        _RippleBandWidth ("Ripple Band Width", Range(0.05, 1)) = 0.25
        _RippleWaveCount ("Ripple Wave Count", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:disp addshadow tessellate:tessEdge
        #pragma target 4.6

        #define MAX_RIPPLES 8

        struct appdata
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct Input
        {
            float dummy;
        };

        fixed4 _Color;
        float _Tessellation;
        float _RippleHeight;
        float _RipplePulseCount;
        float4 _RipplePulses[MAX_RIPPLES];
        float _RippleAxis;
        float _RippleDirectionSign;
        float _RippleAxisMin;
        float _RippleAxisMax;
        float _RippleBandWidth;
        float _RippleWaveCount;

        float GetAxisComponent(float3 value, float axis)
        {
            if (axis < 0.5)
                return value.x;
            if (axis < 1.5)
                return value.y;
            return value.z;
        }

        float SamplePulseDisplacement(float axisCoord, float progress, float heightScale, float axisMin, float axisMax, float extent)
        {
            float front = _RippleDirectionSign > 0.0
                ? lerp(axisMin, axisMax, progress)
                : lerp(axisMax, axisMin, progress);

            float dist = axisCoord - front;
            float band = max(extent * _RippleBandWidth, 0.0001);
            float envelope = exp(-(dist * dist) / (band * band));
            float packetT = saturate((dist + band) / (2.0 * band));
            float spatialPhase = packetT * _RippleWaveCount * 6.2831853;
            float wave = max(0.0, sin(spatialPhase));

            return wave * envelope * _RippleHeight * heightScale;
        }

        float3 ApplyRippleDisplacement(float3 localPos, float3 localNormal)
        {
            if (_RipplePulseCount < 0.5)
                return localPos;

            float axisMin = _RippleAxisMin;
            float axisMax = _RippleAxisMax;
            float extent = max(axisMax - axisMin, 0.0001);
            float axisCoord = GetAxisComponent(localPos, _RippleAxis);
            float displacement = 0.0;

            for (int i = 0; i < MAX_RIPPLES; i++)
            {
                if (i >= (int)_RipplePulseCount)
                    break;

                float4 pulse = _RipplePulses[i];
                if (pulse.z < 0.5)
                    continue;

                displacement += SamplePulseDisplacement(axisCoord, pulse.x, pulse.y, axisMin, axisMax, extent);
            }

            return localPos + localNormal * displacement;
        }

        float4 tessEdge(appdata v0, appdata v1, appdata v2)
        {
            return _Tessellation;
        }

        void disp(inout appdata v)
        {
            v.vertex.xyz = ApplyRippleDisplacement(v.vertex.xyz, normalize(v.normal));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = 0.55;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
