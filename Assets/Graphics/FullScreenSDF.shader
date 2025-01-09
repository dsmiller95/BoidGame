Shader "Hidden/FullScreenSDF"
{
    Properties
    {
        _BackgroundColor("Background Color", Color) = (0, 0, 0, 1)
    }

    HLSLINCLUDE

    // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
    // Or #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
    // as needed for URP

    // SDF struct (must match the layout in C#)
    struct SDFObjectData
    {
        int     shapeType;
        float4  color;
        float   radius;
        float2  center;
    };

    StructuredBuffer<SDFObjectData> _SDFObjects;
    int _SDFObjectCount;
    float4 _BackgroundColor;

    #define MAX_SDF_OBJECTS 3
    static const SDFObjectData s_SDFObjects[MAX_SDF_OBJECTS] =
    {
        // shapeType, color, radius, center
        { 0, float4(1, 0, 0, 1), 0.25, float2(0.3, 0.5) },
        { 0, float4(0, 1, 0, 1), 0.20, float2(0.5, 0.5) },
        { 0, float4(0, 0, 1, 1), 1000, float2(0.7, 0.5) }
    };

    // Helper
    float GetNormalizedDistanceFromCenter(float2 relPos, float radius)
    {
        return length(relPos) / radius;
    }

    // We'll define a minimal vertex->fragment approach:
    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
    };

    struct Varyings
    {
        float2 uv         : TEXCOORD0;
        float4 positionCS : SV_POSITION;
    };

    Varyings Vert(Attributes IN)
    {
        Varyings OUT;
        OUT.positionCS = IN.positionOS;
        OUT.uv = IN.uv;
        return OUT;
    }

    float4 Frag(Varyings IN) : SV_TARGET
    {
        return _BackgroundColor;
        // uv is 0..1 in screen space
        float2 uv = IN.uv;
        

        float minDist = 1e9;
        float4 finalColor = _BackgroundColor;

        //[unroll]
        for(int i=0; i<MAX_SDF_OBJECTS; i++)
        {
            SDFObjectData obj = s_SDFObjects[i];
            float2 relPos = uv - obj.center;
            float dist    = GetNormalizedDistanceFromCenter(relPos, obj.radius);

            if(dist < 1 && dist < minDist)
            {
                minDist = dist;
                finalColor = obj.color;
            }
        }
        return finalColor;
    }

    

    ENDHLSL

    SubShader
    {
        // "Hidden" indicates we won't see it in the Shader menu.
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "FullScreenSDF"
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            ENDHLSL
        }
    }
}
