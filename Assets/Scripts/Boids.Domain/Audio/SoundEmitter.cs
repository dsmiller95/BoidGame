using System;
using System.Collections.Generic;
using Boids.Domain.Goals;
using Dman.Utilities;
using Dman.Utilities.Logger;
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
        ObstacleDrop,
        ObstaclePickup,
        ButtonClick,
        UiMove,
    }

    [Serializable]
    public struct SoundEffect
    {
        public SoundEffectType type;
        public AudioClip[] clips;
        public float volume;
        public int semiToneOffset;
        public int[] semiTones;
        public int maxConcurrent;
        
        
        public readonly float GetPitch(Random rng)
        {
            var semitone = rng.PickRandom(semiTones) + semiToneOffset;
            return GetSemiTone(1f, semitone);
        }
        
        /// <summary>
        /// Returns a pitch value for a given base pitch shifted by a specified number of semitones.
        /// </summary>
        /// <param name="basePitch">The original pitch.</param>
        /// <param name="semitones">The number of semitones to shift. Positive = higher, negative = lower.</param>
        /// <returns>A float representing the new pitch.</returns>
        private static float GetSemiTone(float basePitch, int semitones)
        {
            return basePitch * Mathf.Pow(2f, semitones / 12f);
        }
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
        public int MaxEvents { get; }
    }
    
    [UnitySingleton]
    public class SoundEmitter : MonoBehaviour, IEmitSoundEffects
    {
        public SoundEffect[] soundEffects;
        public AudioSource audioSourcePrefab;
        public int totalSources = 8;

        [Serializable]
        private class AudioPlayingSource
        {
            public AudioSource source;
            public SoundEffectType? lastType;
            public SoundEffect? lastEffect;
            public int totalPlaying;


            public AudioPlayingSource(AudioSource source)
            {
                this.source = source;
                lastType = null;
                lastEffect = null;
                totalPlaying = 0;
            }
            
            public bool IsPlaying()
            {
                return source.isPlaying;
            }
            
            public bool IsPlaying(SoundEffectType type)
            {
                if (!IsPlaying()) return false;
                
                return lastType == type;
            }

            public bool Play(SoundEffectEmit emit, SoundEffect effect, Random rng)
            {
                if (this.IsPlaying(emit.type))
                {
                    return PlayOverlap(emit.type);
                }
                else
                {
                    return PlayNew(emit, effect, rng);
                }
            }

            private bool PlayNew(SoundEffectEmit emit, SoundEffect effect, Random rng)
            {
                lastEffect = effect;
                lastType = emit.type;
                totalPlaying = 1;
                
                source.clip = rng.PickRandom(effect.clips);
                source.transform.position = new Vector3(emit.position.x, emit.position.y, 0);
                source.pitch = effect.GetPitch(rng);
                
                source.volume = lastEffect.Value.volume;
                source.Play();
                
                return true;
            }

            public bool PlayOverlap(SoundEffectType emit)
            {
                if (!this.IsPlaying(emit))
                {
                    Log.Error("tried to play overlap on a source that is not playing the same sound");
                    return false;
                }
                
                if(lastEffect == null)
                {
                    Log.Error("did not get last effect");
                    return false;
                }

                this.totalPlaying++;
                this.source.volume = lastEffect.Value.volume * totalPlaying;
                return true;
            }
            
        }
        
        [SerializeField]
        private AudioPlayingSource[] audioSources;

        public int MaxEvents => totalSources;

        private EntityQuery _soundQuery;
        
        private void Awake()
        {
            if(SingletonLocator<SoundEmitter>.Instance != this)
            {
                Destroy(this);
                return;
            }
            
            DontDestroyOnLoad(this);
            audioSources = new AudioPlayingSource[totalSources];
            for (int i = 0; i < totalSources; i++)
            {
                var source = Instantiate(audioSourcePrefab, transform);
                source.playOnAwake = false;
                source.loop = false;
                audioSources[i] = new AudioPlayingSource(source);
            }
        }
        
        

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            using var sounds = EmitSoundWhenJerk.GetSoundData(world);
            this.EmitSounds(sounds);
            using var sounds2 = EmitSoundWhenDrag.GetSoundData(world);
            this.EmitSounds(sounds2);
        }


        public void EmitSounds(NativeSlice<EmittedSound> sounds)
        {
            var rng = new Random(UnityEngine.Random.Range(1, int.MaxValue));
            foreach (EmittedSound emit in sounds)
            {
                EmitEffect(emit.soundEffectEmit, rng);
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
            var sourceIndex = GetAvailableSource();
            if (sourceIndex == null)
            {
                var alreadyPlayingSourceIndex = GetRandomSourcePlaying(emit.type, rng);
                if (!alreadyPlayingSourceIndex.HasValue) return;
                var alreadyPlayingSource = audioSources[alreadyPlayingSourceIndex.Value];
                alreadyPlayingSource.PlayOverlap(emit.type);
                return;
            }
            
            var effect = Array.Find(soundEffects, e => e.type == emit.type);
            if(effect.Equals(default)) return;
            
            var source = GetBestFor(effect, rng);
            if (source == null) return;
            source.Play(emit, effect, rng);
        }

        private AudioPlayingSource? GetBestFor(SoundEffect effect, Random rng)
        {
            var playingSources = new List<AudioPlayingSource>();
            
            foreach (AudioPlayingSource source in audioSources)
            {
                if (source.IsPlaying(effect.type))
                {
                    playingSources.Add(source);
                }
            }

            if (playingSources.Count >= effect.maxConcurrent)
            {
                return rng.PickRandom(playingSources);
            }
            
            foreach (AudioPlayingSource source in audioSources)
            {
                if (!source.IsPlaying())
                {
                    return source;
                }
            }

            return null;
        }


        
        private int? GetAvailableSource()
        {
            for (var index = 0; index < audioSources.Length; index++)
            {
                var source = audioSources[index];
                if (!source.IsPlaying())
                {
                    return index;
                }
            }

            return null;
        }
        
        private int? GetRandomSourcePlaying(SoundEffectType type, Random rng)
        {
            var playingSources = new List<int>();
            for (var index = 0; index < audioSources.Length; index++)
            {
                var source = audioSources[index];
                if (source.IsPlaying(type))
                {
                    playingSources.Add(index);
                }
            }

            if (playingSources.Count == 0) return null;
            return rng.PickRandom(playingSources);
        }
    }
}