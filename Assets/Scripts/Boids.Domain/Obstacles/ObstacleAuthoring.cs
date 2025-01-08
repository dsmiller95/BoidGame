using System;
using Boids.Domain.GridSnap;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Boids.Domain.Obstacles
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Range(0, 1)]
        public float hardSurfaceRadiusFraction = 0.8f;
        
        [FormerlySerializedAs("variantData")] 
        public ObstacleBehavior behavior = new ObstacleBehavior()
        {
            variant = ObstacleBehaviorVariant.Repel,
            obstacleEffectMultiplier = 1f,
            maxEffectMagnitude = 10f,
        };
        public ObstacleShapeDataDefinition shapeData = new ObstacleShapeDataDefinition()
        {
            shapeVariant = ObstacleShapeVariant.Sphere,
            obstacleRadius = 1f,
            obstacleSecondarySize = 1f,
        };
        [FormerlySerializedAs("draggable")] public bool playerOwned = false;
        
        [SerializeField] private int gizmoDrawResolution = 20;
        [Range(0, 1)]
        [SerializeField] private float gizmosDrawTransparency = .7f;

        public static bool Migrate(ObstacleAuthoring component)
        {
            return false;
        }
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new ObstacleComponent()
                {
                    behavior = authoring.behavior,
                    shapeData = authoring.shapeData,
                    obstacleHardSurfaceRadiusFraction = authoring.hardSurfaceRadiusFraction,
                });
                var spriteRenderer = GetComponent<SpriteRenderer>();
                AddComponent(entity, new OriginalColor
                {
                    Color = spriteRenderer.color.ToFloat4()
                });
                if (authoring.playerOwned)
                {
                    AddComponent(entity, new DraggableObstacle());
                    AddComponent(entity, new ScoringObstacleFlag());
                    AddComponent(entity, new SnapMeToGridFlag());
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var obstacleComponent = new ObstacleComponent()
            {
                behavior = behavior,
                shapeData = shapeData,
                obstacleHardSurfaceRadiusFraction = hardSurfaceRadiusFraction,
            };
            
            var localToWorld = new LocalToWorld
            {
                Value = float4x4.TRS(float3.zero, this.transform.rotation, this.transform.lossyScale)
            };
            var obstacle = obstacleComponent.GetWorldSpace(localToWorld);
            var maxExtent = obstacle.shape.MaximumExtent();
            var minLocal = -new Vector2(maxExtent, maxExtent);
            var maxLocal = new Vector2(maxExtent, maxExtent);
            var resolution = new Vector2Int(gizmoDrawResolution, gizmoDrawResolution);
            SampleSdfGizmos(minLocal, maxLocal, resolution, localPoint =>
            {
                var normalizedDistance = obstacle.shape.GetNormalizedDistance(localPoint);
                if (normalizedDistance > 1) return null;
                var color = Color.green;
                if (obstacle.IsInsideHardSurface(normalizedDistance))
                {
                    color = Color.red;
                }

                //color.b = normalizedDistance;
                
                return Color.Lerp(Color.clear, color, gizmosDrawTransparency);
            });
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