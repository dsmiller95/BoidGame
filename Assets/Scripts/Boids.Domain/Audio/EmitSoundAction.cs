using Dman.Utilities;
using UnityEngine;

namespace Boids.Domain.Audio
{
    public class EmitSoundAction : MonoBehaviour
    {
        public SoundEffectType soundType;
        
        public void EmitSound()
        {
            var soundEmit = new SoundEffectEmit()
            {
                type = soundType,
                position = transform.position.ToFloat3().xy,
            };
            var soundEmitter = SingletonLocator<IEmitSoundEffects>.Instance;
            soundEmitter.EmitSounds(new[] {soundEmit});
        }
    }
}