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
            static int BOX = 2;
            
            struct SdfVariantData
            {
                int shapeType;
                float4 unionData;
            };
            
            struct SDFObjectData
            {
                float radius;
                float hardRadius;
                float annularRadius;
                uint objectFlags;
                float2 center;
                float4 color;

                SdfVariantData variantData;
            };
            

            struct CircleVariant
            {
                
            };
            CircleVariant AsCircleVariant(SdfVariantData variantData)
            {
                CircleVariant c;
                return c;
            }
            
            float CircleDistance(float2 p, CircleVariant circle)
            {
                return length(p);
            }
            
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
            float BeamDistance(float2 p, BeamVariant beam)
            {
                float2 a = float2(0, 0);
                float2 b = beam.beamRelativeEnd;
                
                float2 ba = b - a;
                float2 pa = p - a;
                float2 h = clamp(dot(pa, ba) / dot(ba, ba), 0, 1);
                return length(pa - h * ba);
            }

            struct BoxVariant
            {
                float2 corner;
            };
            BoxVariant AsBoxVariant(SdfVariantData variantData)
            {
                BoxVariant s;
                s.corner = variantData.unionData.xy;
                return s;
            }
            float BoxDistance(float2 p, BoxVariant box)
            {
                float2 b = box.corner;
                
                float2 d = abs(p) - b;
                return length(max(d, 0.0f)) + min(max(d.x, d.y), 0.0f);
            }

            StructuredBuffer<SDFObjectData> _SDFObjects;
            int _SDFObjectCount;

            float GetDistanceFromCenter(float2 relPos, SdfVariantData variantData)
            {
                int st = variantData.shapeType;

                if (st == SPHERE)
                {
                    CircleVariant circle = AsCircleVariant(variantData);
                    return CircleDistance(relPos, circle);
                }
                if (st == BEAM)
                {
                    BeamVariant beam = AsBeamVariant(variantData);
                    return BeamDistance(relPos, beam);
                }
                if (st == BOX)
                {
                    BoxVariant box = AsBoxVariant(variantData);
                    return BoxDistance(relPos, box);
                }
                return 10000;
            }

            float GetDistance(
                float2 relPos,
                SDFObjectData objectData)
            {
                
                float dist = GetDistanceFromCenter(relPos, objectData.variantData);
                if (objectData.annularRadius > 0)
                {
                    dist = dist - objectData.radius;
                    dist = abs(dist) - objectData.annularRadius;
                    dist = dist + objectData.radius;
                }
                return dist;
            }
            
            float GetNormalizedDistanceFromCenter(
                float2 relPos,
                SDFObjectData objectData)
            {
                float dist = GetDistance(relPos, objectData);
                return dist / objectData.radius;
            }

            struct SdfHit
            {
                int objIndex;
                float distance;
                float normalizedDistance;
                float hardRadius;
                float4 color;

                void accumulate(SdfHit other)
                {
                    if(other.normalizedDistance >= normalizedDistance) return;

                    objIndex = other.objIndex;
                    distance = other.distance;
                    normalizedDistance = other.normalizedDistance;
                    hardRadius = other.hardRadius;
                    color = other.color;
                }

                bool isHit()
                {
                    return objIndex >= 0;
                }
            };

            void Unity_RandomRange_float(float2 Seed, float Min, float Max, out float Out)
            {
                float randomno =  frac(sin(dot(Seed, float2(12.9898, 78.233)))*43758.5453);
                Out = lerp(Min, Max, randomno);
            }
            
            float4 Render(float4 backgroundColor, SdfHit sdfHit, SDFObjectData hitObject)
            {
                float radius = sdfHit.distance / sdfHit.normalizedDistance;
                
                if(sdfHit.distance > radius) return backgroundColor;

                if(sdfHit.distance < sdfHit.hardRadius)
                {
                    return sdfHit.color;
                }

                float speedAdj = 1;
                //Unity_RandomRange_float(float2(sdfHit.color.x, sdfHit.objIndex) , .9, 1.1, speedAdj);

                int reverseFlow = (hitObject.objectFlags & 1) ? -1 : 1;
                float t = sdfHit.distance + _Time.y * -4 * reverseFlow * speedAdj;
                
                float normalizedDistance = (sdfHit.distance - sdfHit.hardRadius) / (radius - sdfHit.hardRadius);
                float alphaFromEdge = (1 - normalizedDistance);
                float ridges = (sin(t * 2) + 1) / 2;
                float alpha = alphaFromEdge * (ridges * 0.7 + 0.3);
                
                float4 objectColor = sdfHit.color;
                objectColor.a = min(objectColor.a, alpha);
                return objectColor;
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
                float2 uv = i.uv;
                
                SdfHit hit = {-1, 1e9, 1e9, 1, _BackgroundColor};

                //[unroll]
                for (int index = 0; index < _SDFObjectCount; index++)
                {
                    SDFObjectData obj = _SDFObjects[index];
                    float2 relPos = uv - obj.center;
                    float dist = GetDistance(relPos, obj);
                    float normalDist = dist / obj.radius;

                    SdfHit currentHit = {
                        index,
                        dist,
                        normalDist,
                        obj.hardRadius,
                        obj.color
                    };

                    hit.accumulate(currentHit);
                }
                if(hit.isHit())
                {
                    SDFObjectData hitObject = _SDFObjects[hit.objIndex];
                    return Render(_BackgroundColor, hit, hitObject);
                }
                return _BackgroundColor;
            }
            ENDCG
        }
    }
}