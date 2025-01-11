using System;
using Boids.Domain.Goals;
using Dman.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

namespace Boids.Domain.Audio
{

    public enum SoundEffectType
    {
        Ding,
    }

    [Serializable]
    public struct SoundEffect
    {
        public SoundEffectType type;
        public AudioClip[] clips;
        public float volume;
        public int[] semiTones;
    }

    [Serializable]
    public struct SoundEffectEmit
    {
        public SoundEffectType type;
        public float2 position;
    }

    public interface IEmitSoundEffects
    {
        public void EmitSounds(SoundEffectEmit[] sounds);
        public void EmitSounds(NativeSlice<SoundEffectEmit> sounds);
        public int MaxEvents { get; }
    }
    
    [UnitySingleton]
    public class SoundEmitter : MonoBehaviour, IEmitSoundEffects
    {
        public SoundEffect[] soundEffects;
        public AudioSource audioSourcePrefab;
        public int totalSources = 8;
        
        private AudioSource[] audioSources;

        public int MaxEvents => totalSources;

        private void Awake()
        {
            audioSources = new AudioSource[totalSources];
            for (int i = 0; i < totalSources; i++)
            {
                var source = Instantiate(audioSourcePrefab, transform);
                source.playOnAwake = false;
                audioSources[i] = source;
            }
        }

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            using var sounds = EmitSoundWhenJerk.GetSoundData(world);
            this.EmitSounds(sounds);
        }


        public void EmitSounds(NativeSlice<SoundEffectEmit> sounds)
        {
            var rng = new Random(UnityEngine.Random.Range(1, int.MaxValue));
            foreach (SoundEffectEmit emit in sounds)
            {
                EmitEffect(emit, rng);
            }
        }
        public void EmitSounds(SoundEffectEmit[] sounds)
        {
            var rng = new Random(UnityEngine.Random.Range(1, int.MaxValue));
            foreach (SoundEffectEmit emit in sounds)
            {
                EmitEffect(emit, rng);
            }
        }

        private void EmitEffect(SoundEffectEmit emit, Random rng)
        {
            var source = GetAvailableSource();
            if(source == null) return;
            var effect = Array.Find(soundEffects, e => e.type == emit.type);
            if(effect.Equals(default)) return;
                
            source.clip = rng.PickRandom(effect.clips);
            source.transform.position = new Vector3(emit.position.x, emit.position.y, 0);
            source.volume = effect.volume;
            var semitone = rng.PickRandom(effect.semiTones);
            source.pitch = GetSemiTone(1f, semitone);
                
            source.Play();
        }


        /// <summary>
        /// Returns a pitch value for a given base pitch shifted by a specified number of semitones.
        /// </summary>
        /// <param name="basePitch">The original pitch.</param>
        /// <param name="semitones">The number of semitones to shift. Positive = higher, negative = lower.</param>
        /// <returns>A float representing the new pitch.</returns>
        private float GetSemiTone(float basePitch, int semitones)
        {
            return basePitch * Mathf.Pow(2f, semitones / 12f);
        }
        
        private AudioSource? GetAvailableSource()
        {
            foreach (var source in audioSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null;
        }
    }
}