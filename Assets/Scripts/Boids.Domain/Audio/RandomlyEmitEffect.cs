using System;
using Dman.Utilities;
using UnityEngine;

namespace Boids.Domain.Audio
{
    public class RandomlyEmitEffect : MonoBehaviour
    {
        public SoundEffectType type;
        public float chance = 1 / 60f;

        private void FixedUpdate()
        {
            if (UnityEngine.Random.value < chance)
            {
                var emitter = SingletonLocator<IEmitSoundEffects>.Instance;
                emitter.EmitSounds(new[] { new SoundEffectEmit
                {
                    type = type,
                    position = transform.position.ToFloat3().xy,
                } });
            }
        }
    }
}