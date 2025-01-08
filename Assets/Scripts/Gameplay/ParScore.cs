using Boids.Domain.Obstacles;
using Unity.Entities;
using UnityEngine;

namespace Gameplay
{
    public class ParScore : MonoBehaviour
    {
        [SerializeField]
        private TMPro.TMP_Text scoreText;

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var score = ObstacleScoringSystem.GetScoringObstacleData(world);
            
            scoreText.text = $"Used: {score.totalScoringObstacles}";
        }
    }
}