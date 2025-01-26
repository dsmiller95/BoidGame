using System;
using Boids.Domain.Audio;
using Boids.Domain.GridSnap;
using Boids.Domain.Obstacles.ComposedObstacles;
using Boids.Domain.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Boids.Domain.Obstacles
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Obsolete]
        public float hardSurfaceRadiusFraction = 0.8f;
        public float hardSurfaceRadius = 0.8f;
        
        [FormerlySerializedAs("variantData")] 
        public ObstacleBehavior behavior = new ObstacleBehavior()
        {
            variant = ObstacleBehaviorVariant.Repel,
            obstacleEffectMultiplier = 1f,
            maxEffectMagnitude = 10f,
        };
        public ShapeDataDefinition shapeData = new ShapeDataDefinition()
        {
            shapeVariant = ShapeVariant.Sphere,
            obstacleRadius = 1f
        };
        [FormerlySerializedAs("draggable")] public bool playerOwned = false;
        public bool snapToGrid = false;
        public bool emitSounds = true;
        public SoundEffectType emitWhenPickedUp = SoundEffectType.Ding;
        public SoundEffectType emitWhenDropped = SoundEffectType.Ding;
        
        [SerializeField] private int gizmoDrawResolutionUnselected = 10;
        [SerializeField] private float gizmosDrawTransparencyUnselected = .1f;
        
        [SerializeField] private int gizmoDrawResolution = 20;
        [Range(0, 1)]
        [SerializeField] private float gizmosDrawTransparency = .7f;

        public static bool Migrate(ObstacleAuthoring component)
        {
            var hardRadius = component.hardSurfaceRadiusFraction * component.shapeData.obstacleRadius;
            if(!Mathf.Approximately(component.hardSurfaceRadius, hardRadius))
            {
                component.hardSurfaceRadius = hardRadius;
                return true;
            }

            return false;
        }
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new ObstacleComponent()
                {
                    behavior = authoring.behavior,
                    hardRadius = authoring.hardSurfaceRadius,
                });
                AddComponent(entity, new SdfShapeComponent()
                {
                    shapeData = authoring.shapeData,
                });
                
                var childControlPoint = GetComponentInChildren<ObstacleControlPointAuthoring>();
                if (childControlPoint != null)
                {
                    AddComponent(entity, new CompositeObstacleFlag());
                }
                var spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // we may not render the obstacle
                    var originalColor = spriteRenderer.color.ToFloat4();
                    AddComponent(entity, new OriginalColor
                    {
                        Color = originalColor,
                    });
                    AddComponent(entity, new ObstacleRender()
                    {
                        color = originalColor,
                    });
                    AddComponent(entity, new SDFObjectRenderData()
                    {
                        color = originalColor,
                    });
                }
                if (authoring.playerOwned)
                {
                    AddComponent(entity, DraggableSdf.Default);
                    AddComponent(entity, IsDragging.Default);
                    AddComponent(entity, WasDragging.Default);
                    AddComponent(entity, new ScoringObstacleFlag());
                    AddComponent(entity, new ObstacleMayDisableFlag());
                }
                if(authoring.snapToGrid || authoring.playerOwned)
                {
                    AddComponent(entity, new SnapMeToGridFlag());
                }
                
                if (authoring.emitSounds)
                {
                    AddComponent(entity, new EmitSoundWhenDragComponent()
                    {
                        pickupSoundType = authoring.emitWhenPickedUp,
                        dropSoundType = authoring.emitWhenDropped,
                    });
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (gizmosDrawTransparency <= 0)
            {
                return;
            }
            
            Obstacle obstacle = GetMyShape();
            var maxExtent = obstacle.shape.MaximumExtent();
            var minLocal = -new Vector2(maxExtent, maxExtent);
            var maxLocal = new Vector2(maxExtent, maxExtent);
            var resolution = new Vector2Int(gizmoDrawResolution, gizmoDrawResolution);
            SampleSdfGizmos(minLocal, maxLocal, resolution, localPoint =>
            {
                var distance = obstacle.shape.GetDistance(localPoint);
                if (!obstacle.shape.IsInside(distance)) return null;
                var color = Color.green;
                if (obstacle.IsInsideHardSurface(distance))
                {
                    color = Color.red;
                }

                //color.b = normalizedDistance;
                
                return Color.Lerp(Color.clear, color, gizmosDrawTransparency);
            });
        }

        private Obstacle GetMyShape()
        {
            var obstacleComponent = new ObstacleComponent()
            {
                behavior = behavior,
                hardRadius = hardSurfaceRadius,
            };
            var shapeComponent = new SdfShapeComponent()
            {
                shapeData = shapeData,
            };
            var localToWorld = new LocalToWorld
            {
                Value = float4x4.TRS(float3.zero, this.transform.rotation, this.transform.lossyScale)
            };
            
            var obstacle = shapeComponent.GetWorldSpace(localToWorld, obstacleComponent);
            return obstacle;
        }

        private void SampleSdfGizmos(
            Vector2 minLocal, Vector2 maxLocal,
            Vector2Int resolution, Func<float2, Color?> sample)
        {
            var step = (maxLocal - minLocal) / resolution;
            for (var x = 0; x < resolution.x; x++)
            {
                for (var y = 0; y < resolution.y; y++)
                {
                    var localPoint = new Vector2(x, y) * step + minLocal;
                    var color = sample(localPoint);
                    if (color.HasValue)
                    {
                        var worldPoint = transform.position + (Vector3)localPoint;
                        Gizmos.color = color.Value;
                        Gizmos.DrawCube(worldPoint, step);
                    }
                }
            }
        }
    }
}