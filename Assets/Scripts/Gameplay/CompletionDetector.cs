using Boids.Domain.Goals;
using Levels;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Gameplay
{
    public class CompletionDetector : MonoBehaviour
    {
        public UnityEvent<bool> onIsCompletedChanged;
        [SerializeField] private bool isCompleted;
        
        public UnityEvent<float> onCompletionAmountChanged;
        public UnityEvent<float> onInverseCompletionAmountChanged;
        
        [FormerlySerializedAs("onCompletionAmountIncrease")] 
        public UnityEvent<float> onUnclampedCompletionAmountIncrease;
        
        [SerializeField] private float completionAmount;
        [SerializeField] private float unclampedCompletionAmount;

        public InLevelActions actions;
        
        private bool _didEmit;
        
        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var score = GoalScoringSystem.GetScoringData(world);
            if (!_didEmit)
            {
                onIsCompletedChanged.Invoke(score.IsCompleted);
                onCompletionAmountChanged.Invoke(score.scoreCompletionPercent);
                onInverseCompletionAmountChanged.Invoke(1 - score.scoreCompletionPercent);
            }
            
            SetIsCompleted(score.IsCompleted);
            SetCompletionAmount(score.scoreCompletionPercent);
            SetUnclampedCompletionAmount(score.unclampedScoreCompletionPercent);
        }
        
        private void SetIsCompleted(bool value)
        {
            if(value == isCompleted) return;
            isCompleted = value;
            
            onIsCompletedChanged.Invoke(isCompleted);
            if (isCompleted)
            {
                actions.OnCompletion();
            }
        }
        
        private void SetCompletionAmount(float value)
        {
            if(Mathf.Approximately(value, completionAmount)) return;
            completionAmount = value;
            
            onCompletionAmountChanged.Invoke(value);
            onInverseCompletionAmountChanged.Invoke(1 - value);
        }
        
        private void SetUnclampedCompletionAmount(float value)
        {
            if(Mathf.Approximately(value, unclampedCompletionAmount)) return;
            var delta = value - unclampedCompletionAmount;
            unclampedCompletionAmount = value;
            
            if(delta > 0)
            {
                onUnclampedCompletionAmountIncrease.Invoke(delta);
            }
        }
    }
}