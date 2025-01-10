using System;
using Boids.Domain.OnClick;
using Unity.Entities;
using UnityEngine;

namespace Levels
{
    public class DisableInputWhileEnabled : MonoBehaviour
    {
        private void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var inputSystem = world.GetExistingSystemManaged<InputSystemGroup>();
            inputSystem.Enabled = false;
        }

        private void OnDisable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var inputSystem = world.GetExistingSystemManaged<InputSystemGroup>();
            inputSystem.Enabled = true;
        }
    }
}