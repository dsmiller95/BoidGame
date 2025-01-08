using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Range(1f, 30f)]
        public float obstacleRadius = 1f;
        public float obstacleSecondarySize = 1f;
        public float hardSurfaceRadius = 0.8f;
        public ObstacleShape shape = ObstacleShape.Sphere;

        public ObstacleVariantData variantData = new ObstacleVariantData()
        {
            variant = ObstacleType.Repel,
            obstacleEffectMultiplier = 1f,
            maxEffectMagnitude = 10f,
        };
        public bool draggable = false;
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new ObstacleComponent()
                {
                    variantData = authoring.variantData,
                    shape = authoring.shape,
                    obstacleRadius = authoring.obstacleRadius,
                    obstacleSecondarySize = authoring.obstacleSecondarySize,
                    obstacleHardSurfaceRadius = authoring.hardSurfaceRadius,
                });
                var spriteRenderer = GetComponent<SpriteRenderer>();
                AddComponent(entity, new OriginalColor
                {
                    Color = spriteRenderer.color.ToFloat4()
                });
                if (authoring.draggable)
                {
                    AddComponent(entity, new DraggableObstacle());
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var obstacleComponent = new ObstacleComponent()
            {
                variantData = variantData,
                shape = shape,
                obstacleRadius = obstacleRadius,
                obstacleSecondarySize = obstacleSecondarySize,
                obstacleHardSurfaceRadius = hardSurfaceRadius,
            };
            var linearScale = this.transform.lossyScale.x;
            var rotation = this.transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            var obstacle = obstacleComponent.AdjustForScale(linearScale, rotation);
            
            var maxExtent = Mathf.Max(obstacle.obstacleRadius, obstacle.obstacleRadius + obstacle.obstacleSecondarySize);
            var minLocal = -new Vector2(maxExtent, maxExtent);
            var maxLocal = new Vector2(maxExtent, maxExtent);
            var resolution = new Vector2Int(20, 20);
            SampleSdfGizmos(minLocal, maxLocal, resolution, localPoint =>
            {
                var normalizedDistance = obstacle.GetNormalizedDistance(localPoint);
                if (normalizedDistance > 1) return null;
                var color = Color.green;
                if (obstacle.IsInsideHardSurface(normalizedDistance))
                {
                    color = Color.red;
                }

                //color.b = normalizedDistance;
                
                return Color.Lerp(color, Color.clear, 0.3f);
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