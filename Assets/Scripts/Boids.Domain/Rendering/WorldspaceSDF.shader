Shader "Unlit/FullScreenSDF 2"
{
    Properties
    {
        _BackgroundColor("Background Color", Color) = (0, 1, 0, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        // Transparent objects typically don’t write to depth (ZWrite Off),
        // and use alpha blending (SrcAlpha, OneMinusSrcAlpha).
        Blend SrcAlpha OneMinusSrcAlpha
        //ZWrite Off

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BackgroundColor;


            // enum defining shape types
            static int SPHERE = 0;
            static int BEAM = 1;
            static int SQUARE = 2;
            
            struct SdfVariantData
            {
                int shapeType;
                float4 unionData;
            };
            
            struct SDFObjectData
            {
                float radius;
                float hardRadiusFraction;
                float2 center;
                float4 color;

                SdfVariantData variantData;
            };
            
            struct BeamVariant
            {
                float2 beamRelativeEnd;
            };
            BeamVariant AsBeamVariant(SdfVariantData variantData)
            {
                BeamVariant b;
                b.beamRelativeEnd = variantData.unionData.xy;
                return b;
            }

            struct SquareVariant
            {
                float2 corner;
            };
            SquareVariant AsSquareVariant(SdfVariantData variantData)
            {
                SquareVariant s;
                s.corner = variantData.unionData.xy;
                return s;
            }

            struct CircleVariant
            {
                
            };
            CircleVariant AsCircleVariant(SdfVariantData variantData)
            {
                CircleVariant c;
                return c;
            }

            StructuredBuffer<SDFObjectData> _SDFObjects;
            int _SDFObjectCount;

            float GetDistanceFromCenter(float2 relPos, SdfVariantData variantData)
            {
                int st = variantData.shapeType;

                if (st == SPHERE)
                {
                    CircleVariant circle = AsCircleVariant(variantData);
                    return length(relPos);
                }
                if (st == BEAM)
                {
                    BeamVariant beam = AsBeamVariant(variantData);
                    float2 a = float2(0, 0);
                    float2 b = beam.beamRelativeEnd;
                    float2 p = relPos;
                    
                    float2 ba = b - a;
                    float2 pa = p - a;
                    float h = clamp(dot(pa, ba) / dot(ba, ba), 0, 1);
                    return length(pa - h * ba);
                }
                if (st == SQUARE)
                {
                    SquareVariant square = AsSquareVariant(variantData);
                    return 1;
                }
                return 10000;
            }
            
            float GetNormalizedDistanceFromCenter(float2 relPos, float radius, SdfVariantData variantData)
            {
                float dist = GetDistanceFromCenter(relPos, variantData);
                return dist / radius;
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Standard MVP transform
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Calculate world position
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;

                float _Scale = 1;
                // Use e.g. xz as UVs (or xy, or whatever combination suits your needs)
                o.uv = o.worldPosition.xy * _Scale;

                // If you like, you can still multiply by Unity’s texture Tiling/Offset:
                // o.uv = TRANSFORM_TEX(o.uv, _MainTex);

                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;

                // v2f o;
                // o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // UNITY_TRANSFER_FOG(o,o.vertex);
                // return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //return float4(i.uv.xy, 1, 1);

                float2 uv = i.uv;


                float minDist = 1e9;
                float hardRadiusFraction = 1e9;
                float4 finalColor = _BackgroundColor;

                //[unroll]
                for (int i = 0; i < _SDFObjectCount; i++)
                {
                    SDFObjectData obj = _SDFObjects[i];
                    float2 relPos = uv - obj.center;
                    float dist = GetNormalizedDistanceFromCenter(relPos, obj.radius, obj.variantData);

                    if (dist < 1 && dist < minDist)
                    {
                        minDist = dist;
                        finalColor = obj.color;
                        hardRadiusFraction = obj.hardRadiusFraction;
                    }
                }
                if (minDist < 1)
                {
                    if (minDist > hardRadiusFraction)
                    {
                        finalColor.a = 1 - minDist;
                    }
                }
                
                return finalColor;
                
            }
            ENDCG
        }
    }
}