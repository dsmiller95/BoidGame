using Boids.Domain.Goals;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
    public class CompletionDetector : MonoBehaviour
    {
        public UnityEvent<bool> onIsCompletedChanged;
        [SerializeField] private bool isCompleted;
        
        public UnityEvent<float> onCompletionAmountChanged;
        public UnityEvent<float> onInverseCompletionAmountChanged;
        [SerializeField] private float completionAmount;

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
        }
        
        private void SetIsCompleted(bool value)
        {
            if(value == isCompleted) return;
            isCompleted = value;
            onIsCompletedChanged.Invoke(isCompleted);
        }
        
        private void SetCompletionAmount(float value)
        {
            if(Mathf.Approximately(value, completionAmount)) return;
            completionAmount = value;
            onCompletionAmountChanged.Invoke(value);
            onInverseCompletionAmountChanged.Invoke(1 - value);
        }
    }
}