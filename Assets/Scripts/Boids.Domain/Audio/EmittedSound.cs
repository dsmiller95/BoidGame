using System;
using Unity.Entities;

namespace Boids.Domain.Audio
{
    [InternalBufferCapacity(16)]
    [Serializable]
    public struct EmittedSound : IBufferElementData
    {
        public SoundEffectEmit soundEffectEmit;
        
        public static EmittedSound Create(SoundEffectEmit soundEffectEmit)
        {
            return new EmittedSound
            {
                soundEffectEmit = soundEffectEmit
            };
        }
    }
}