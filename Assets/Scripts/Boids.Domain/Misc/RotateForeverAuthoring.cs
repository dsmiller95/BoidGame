using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Misc
{
    public class RotateForeverAuthoring : MonoBehaviour
    {
        [Tooltip("degrees per second")]
        public float rotationSpeed;

        private class RotateForeverBaker : Baker<RotateForeverAuthoring>
        {
            public override void Bake(RotateForeverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RotateForeverComponent
                {
                    rotationSpeed = math.radians(authoring.rotationSpeed)
                });
            }
        }
    }
}