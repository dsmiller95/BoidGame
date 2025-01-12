using System;
using System.Collections.Generic;
using System.Linq;
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
        GoalCompleteIncrement,
    }

    [Serializable]
    public struct SemitoneLayer
    {
        public int semitoneA;
        public float volumeA;
        
        public int semitoneB;
        public float volumeB;
    }

    [Serializable]
    public struct ShepardToneLayer
    {
        public float pitchA;
        public float volumeA;
        
        public float pitchB;
        public float volumeB;
    }
    
    [Serializable]
    public struct SoundEffect
    {
        public SoundEffectType type;
        public AudioClip[] clips;
        public float volume;
        public int semiToneOffset;
        public int[] semiTones;
        
        [Tooltip("presumes semiTones are sorted in ascending order")]
        [Range(0, 12)]
        public int shepardToneSemitoneOffset;
        
        public int maxConcurrent;
        public bool allowOverlapPlay;
        
        public readonly int PickSemitone(Random rng)
        {
            return rng.PickRandom(semiTones) + semiToneOffset;
        }
        public readonly int PickSemitoneIndex(Random rng)
        {
            return rng.Next(0, semiTones.Length);
        }
        public readonly float GetSemitonePitch(int index)
        {
            var semiTone = semiTones[index] + semiToneOffset;
            return GetSemiTone(1, semiTone);
        }

        public readonly ShepardToneLayer GetShepardTone(int semiToneIndex)
        {
            var minSemitone = semiTones.First() + semiToneOffset;
            var maxSemitone = semiTones.Last() + semiToneOffset + shepardToneSemitoneOffset;
            var allSemitonesMidpoint = (maxSemitone + minSemitone) / 2f;
            var semitonesExtent = (maxSemitone - minSemitone) / 2f;
            
            
            var toneA = semiTones[semiToneIndex] + semiToneOffset;
            var toneB = toneA + shepardToneSemitoneOffset;
            
            var toneANormalized = (toneA - allSemitonesMidpoint) / semitonesExtent;
            var toneBNormalized = (toneB - allSemitonesMidpoint) / semitonesExtent;

            var toneAVolume = GaussianValue(toneANormalized); //  1 - math.abs(toneANormalized);
            var toneBVolume = GaussianValue(toneBNormalized); //  1 - math.abs(toneBNormalized);
            
            return new ShepardToneLayer()
            {
                pitchA = GetSemiTone(1, toneA),
                volumeA = toneAVolume,
                
                pitchB = GetSemiTone(1, toneB),
                volumeB = toneBVolume,
            };
        }
        
        public static float GaussianValue(float x, float sigma = 0.5f)
        {
            // Gaussian function (centered at 0)
            // f(x) = e^(-0.5 * (x/sigma)^2)
            // This peaks at x=0 and tapers off.
            return Mathf.Exp(-0.5f * (x / sigma) * (x / sigma));
        }
        
        public readonly int NextSemitoneIndex(int currentSemitoneIndex)
        {
            return (currentSemitoneIndex + 1) % semiTones.Length;
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
        // when passing in non-zero, this will ensure that all sounds from the same emitter will be played on the same source
        public int emitterId;
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
            public AudioSource overlaySource;
            public SoundEffectType? lastType;
            public SoundEffect? lastEffect;
            public int lastEmitterId;
            public int totalPlaying;

            private int lastSemitoneIndexPlayed;


            public AudioPlayingSource(AudioSource source, AudioSource overlaySource)
            {
                this.source = source;
                this.overlaySource = overlaySource;
                lastType = null;
                lastEffect = null;
                totalPlaying = 0;
                lastEmitterId = 0;
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
                this.lastEmitterId = emit.emitterId;
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
                
                var clip = rng.PickRandom(effect.clips);
                
                

                source.clip = clip;
                source.transform.position = new Vector3(emit.position.x, emit.position.y, 0);
                
                if (effect.shepardToneSemitoneOffset > 0)
                {
                    overlaySource.clip = clip;
                    overlaySource.transform.position = new Vector3(emit.position.x, emit.position.y, 0);
                    
                    
                    var semiToneIndex = effect.NextSemitoneIndex(lastSemitoneIndexPlayed);
                    lastSemitoneIndexPlayed = semiToneIndex;
                    
                    var shepardTone = effect.GetShepardTone(semiToneIndex);
                    source.pitch = shepardTone.pitchA;
                    source.volume = lastEffect.Value.volume * shepardTone.volumeA;
                    source.Play();
                    
                    overlaySource.pitch = shepardTone.pitchB;
                    overlaySource.volume = lastEffect.Value.volume * shepardTone.volumeB;
                    overlaySource.Play();
                }
                else
                {
                    
                    var semiToneIndex = effect.PickSemitoneIndex(rng);
                    lastSemitoneIndexPlayed = semiToneIndex;
                    var pitch = effect.GetSemitonePitch(semiToneIndex);
                    
                    source.pitch = pitch;
                    source.volume = lastEffect.Value.volume;
                    source.Play();
                }
                
                
                
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
            
            transform.SetParent(null);
            DontDestroyOnLoad(this);
            audioSources = new AudioPlayingSource[totalSources];
            for (int i = 0; i < totalSources; i++)
            {
                audioSources[i] = new AudioPlayingSource(CreateSource(), CreateSource());
            }

            AudioSource CreateSource()
            {
                var source = Instantiate(audioSourcePrefab, transform);
                source.playOnAwake = false;
                source.loop = false;
                return source;
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
            var effect = Array.Find(soundEffects, e => e.type == emit.type);
            if(effect.Equals(default)) return;
            
            var source = GetBestFor(effect, emit.emitterId, rng);
            if (source == null) return;
            source.Play(emit, effect, rng);
        }

        private AudioPlayingSource? GetBestFor(SoundEffect effect, int emitterId, Random rng)
        {
            if (emitterId != 0)
            {
                // if we were provided an emitterID, passthrough to the same source
                var affinitySource = audioSources.FirstOrDefault(x => x.lastEmitterId == emitterId && (x.IsPlaying(effect.type) || !x.IsPlaying()));
                if (affinitySource != null)
                {
                    // already playing, and can't overlap. we're done
                    if (!effect.allowOverlapPlay && affinitySource.IsPlaying()) return null;
                    return affinitySource;
                }
            }
            else
            {
                // if we don't have emitterId affinity, pick whichever source is already playing this sound, if we are above the max concurrency
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
                    return effect.allowOverlapPlay ? rng.PickRandom(playingSources) : null;
                }
            }
            
            // if all else fails, pick the first available source
            
            foreach (AudioPlayingSource source in audioSources)
            {
                if (!source.IsPlaying())
                {
                    return source;
                }
            }

            return null;
        }
    }
}