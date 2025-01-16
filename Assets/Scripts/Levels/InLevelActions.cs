using Analytics;
using Boids.Domain.Goals;
using Boids.Domain.Obstacles;
using Cysharp.Threading.Tasks;
using Dman.Utilities;
using Unity.Entities;
using UnityEngine;

namespace Levels
{
    public class InLevelActions : MonoBehaviour
    {
        public TMPro.TMP_Text labelText;

        public GameObject gameWinScreenDisplay;
        public TMPro.TMP_Text gameWinText;

        private AsyncFnOnceCell _uiOnceCell;
        private bool hasCompleted = false;
        
        private LevelData _levelData;
        public void InitializeWith(LevelData levelData)
        {
            _levelData = levelData;
            labelText.text = levelData.SetupData.levelName + "\nPar: " + levelData.SetupData.par;
            CustomAnalytics.LogLevelStart(levelData.LevelIndexId);
        }

        private void Awake()
        {
            _uiOnceCell = new AsyncFnOnceCell(gameObject);
            gameWinScreenDisplay.SetActive(false);
            hasCompleted = false;
        }

        public void RestartLevel()
        {
            SingletonLocator<IManageLevels>.Instance.RestartLevel();
        }

        private LevelCompletionData? lastCompletionData = null;
        
        public void OnCompletion()
        {
            if(hasCompleted)
            {
                Debug.LogWarning("level has already been completed");
                return;
            }

            hasCompleted = true;
            
            
            var world = World.DefaultGameObjectInjectionWorld;
            var scoredGoals = GoalScoringSystem.GetScoringData(world);
            if (!scoredGoals.IsCompleted)
            {
                Debug.LogWarning("marking level as completed but goal scoring system does not indicate completion");
            }
            
            var score = GetScore(world);
            
            lastCompletionData = score;

            var levelManager = SingletonLocator<IManageLevels>.Instance;
            if (levelManager == null)
            { 
                Debug.LogWarning("level manager is null. playing in editor?");
                return;
            }
            
            levelManager.CompleteLevel(_levelData.LevelIndexId, score);
            CustomAnalytics.LogLevelCompletion(_levelData.LevelIndexId, score.usedObstacles);
            
            gameWinScreenDisplay.SetActive(true);
            
            var gameWinMessage = $"Scored {score.usedObstacles}\n" +
                                 $"Par {_levelData.SetupData.par}";

            var flavor = GetFlavor(score.usedObstacles, _levelData.SetupData.par);
            if (flavor.HasValue)
            {
                gameWinMessage += $"\n\n{flavor.Value}!";
            }
            
            gameWinText.text = gameWinMessage;
            
            
            
            //this._uiOnceCell.TryRun(c => DoCompletionAsync(score), "warn");
        }

        private LevelCompletionData GetScore(World world)
        {
            var scoredObstacles = ObstacleScoringSystem.GetScoringObstacleData(world);
            return new LevelCompletionData
            {
                usedObstacles = scoredObstacles.totalScoringObstacles
            };
        }

        private ScoreFlavor? GetFlavor(int score, int par)
        {
            if (score <= 1)
            {
                return ScoreFlavor.Ace;
            }

            var overPar = score - par;

            return overPar switch
            {
                -4 => ScoreFlavor.Condor,
                -3 => ScoreFlavor.Albatross,
                -2 => ScoreFlavor.Eagle,
                -1 => ScoreFlavor.Birdie,
                0 => ScoreFlavor.Par,
                1 => ScoreFlavor.Bogey,
                2 => ScoreFlavor.DoubleBogey,
                3 => ScoreFlavor.TripleBogey,
                4 => ScoreFlavor.QuadrupleBogey,
                _ => null
            };
        }

        private enum ScoreFlavor
        {
            QuadrupleBogey,
            TripleBogey,
            DoubleBogey,
            Bogey,
            Par,
            Ace,
            Birdie,
            Eagle,
            Albatross,
            Condor,
        }
        
        public void NextLevel()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var scoredGoals = GoalScoringSystem.GetScoringData(world);

            LevelCompletionData score;
            if (scoredGoals.IsCompleted)
            {
                score = GetScore(world);
            }
            else
            {
                if (!lastCompletionData.HasValue)
                {
                    Debug.LogWarning("marking level as completed but goal scoring system does not indicate completion");
                    return;
                }

                score = lastCompletionData.Value;
            }

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
                var score = GetScore(world);
                
                var levelManager = SingletonLocator<IManageLevels>.Instance;
                levelManager.CompleteLevel(_levelData.LevelIndexId, score);
            }
            
            SingletonLocator<IManageLevels>.Instance.ExitLevel();
            CustomAnalytics.LogLevelExit(_levelData.LevelIndexId);
        }
    }
}