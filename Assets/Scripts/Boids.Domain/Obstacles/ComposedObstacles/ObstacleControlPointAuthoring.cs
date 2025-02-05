﻿using System;
using Boids.Domain.Audio;
using Boids.Domain.GridSnap;
using Boids.Domain.Rendering;
using Dman.Utilities.Logger;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Boids.Domain.Obstacles.ComposedObstacles
{
    public class ObstacleControlPointAuthoring : MonoBehaviour
    {
        public ShapeDataDefinition shapeData = new ShapeDataDefinition()
        {
            shapeVariant = ShapeVariant.Sphere,
            obstacleRadius = 1f
        };

        public bool limitDistance = false;
        public float maximumDistance = 3f;

        public Color color;
        
        [FormerlySerializedAs("draggable")] public bool playerOwned = false;
        public bool snapToGrid = false;
        public bool emitSounds = true;
        public SoundEffectType emitWhenPickedUp = SoundEffectType.Ding;
        public SoundEffectType emitWhenDropped = SoundEffectType.Ding;
        
        
        private class ComposedObstacleBaker : Baker<ObstacleControlPointAuthoring>
        {
            public override void Bake(ObstacleControlPointAuthoring authoring)
            {
                if (!authoring.enabled) return;
                var parent = this.GetParent();
                var parentAuthoring = GetComponent<ObstacleAuthoring>(parent);
                if (parent == null || parentAuthoring == null)
                {
                    Log.Error("ComposedObstacle must be a child of an Obstacle");
                    return;
                }
                
                var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new SdfShapeComponent()
                {
                    shapeData = authoring.shapeData,
                });
                if (authoring.limitDistance)
                {
                    var limitDistanceComponent = LimitDistanceComponent.Default;
                    limitDistanceComponent.maximumDistance = authoring.maximumDistance;
                    limitDistanceComponent.Validate();
                    AddComponent(entity, limitDistanceComponent);
                }

                if (authoring.color != Color.clear)
                { 
                    var originalColor = authoring.color.ToFloat4();
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
                    AddComponent(entity, DraggableSdf.HighPriority);
                    AddComponent(entity, IsDragging.Default);
                    AddComponent(entity, WasDragging.Default);
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
    }
}