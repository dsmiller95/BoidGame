using Boids.Domain.Goals;
using Boids.Domain.Obstacles;
using Dman.Utilities;
using Unity.Entities;
using UnityEngine;

namespace Levels
{
    public class InLevelActions : MonoBehaviour
    {
        public TMPro.TMP_Text labelText;

        private LevelData _levelData;
        public void InitializeWith(LevelData levelData)
        {
            _levelData = levelData;
            labelText.text = levelData.SetupData.levelName + "\nPar: " + levelData.SetupData.par;
        }
        
        public void RestartLevel()
        {
            SingletonLocator<IManageLevels>.Instance.RestartLevel();
        }

        public void NextLevel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var scoredObstacles = ObstacleScoringSystem.GetScoringObstacleData(world);
            var scoredGoals = GoalScoringSystem.GetScoringData(world);
            if (!scoredGoals.IsCompleted)
            {
                Debug.LogWarning("marking level as completed but goal scoring system does not indicate completion");
            }
            var score = new LevelCompletionData
            {
                usedObstacles = scoredObstacles.totalScoringObstacles
            };
            
            var levelManager = SingletonLocator<IManageLevels>.Instance;
            levelManager.CompleteLevel(_levelData.LevelIndexId, score);
            levelManager.NextLevel();
        }
        
        public void ExitLevel()
        {
            
            var world = World.DefaultGameObjectInjectionWorld;
            var scoredGoals = GoalScoringSystem.GetScoringData(world);
            if (scoredGoals.IsCompleted)
            {
                var scoredObstacles = ObstacleScoringSystem.GetScoringObstacleData(world);
                var score = new LevelCompletionData
                {
                    usedObstacles = scoredObstacles.totalScoringObstacles
                };
                
                var levelManager = SingletonLocator<IManageLevels>.Instance;
                levelManager.CompleteLevel(_levelData.LevelIndexId, score);
            }
            
            SingletonLocator<IManageLevels>.Instance.ExitLevel();
        }
    }
}