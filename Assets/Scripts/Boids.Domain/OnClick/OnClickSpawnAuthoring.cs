using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.OnClick
{
    public struct OnClickSpawn : IComponentData
    {
        public Entity Prefab;
        public LocalTransform defaultTransform;
    }
    
    public class OnClickSpawnAuthoring : MonoBehaviour
    {
        public GameObject prefab;

        private class OnClickSpawnBaker : Baker<OnClickSpawnAuthoring>
        {
            public override void Bake(OnClickSpawnAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                var entityPrefab = GetEntity(authoring.prefab, TransformUsageFlags.Renderable);
                var prefabTransform = LocalTransform.FromMatrix(authoring.prefab.transform.localToWorldMatrix);
                AddComponent(entity, new OnClickSpawn
                {
                    Prefab = entityPrefab,
                    defaultTransform = prefabTransform
                });
            }
        }
    }
}