using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dman.SimpleJson;
using Dman.Utilities;
using Dman.Utilities.Logger;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Levels
{
    public interface IManageLevels
    {
        public void RestartLevel();
        public void NextLevel();
        public void CompleteLevel(int levelIndexId, LevelCompletionData completionData);
        public void ExitLevel();
        
        public void LoadLevelByIndex(int levelIndexId);
        
        public IEnumerable<LevelData> GetLevelData();
    }

    public struct LevelCompletionData
    {
        public int usedObstacles;
    }
    
    public class LevelData
    {
        public int LevelIndexId;
        public LevelSaveData? SaveData;
        public LevelSetupData SetupData;
    }

    [Serializable]
    public class LevelsSaveData
    {
        public static LevelsSaveData Empty => new LevelsSaveData
        {
            levelData = new List<LevelSaveData>()
        };
        
        public List<LevelSaveData> levelData;
    }
    
    [Serializable]
    public class LevelSaveData
    {
        public static LevelSaveData Empty => new LevelSaveData
        {
            best = -1
        };
        /// <summary>
        /// -1 if not completed
        /// </summary>
        public int best;
    }

    [Serializable]
    public struct LevelSetupData
    {
        public string levelName;
        public int par;
    }
    
    [UnitySingleton]
    public class LevelManager : MonoBehaviour, IManageLevels
    {
        [Serializable]
        private struct Level
        {
            public LevelSetupData metadata;
            public SceneReference sceneReference;
        }
        
        [SerializeField] private Level[] levels;
        [SerializeField] private int currentLevel;
        [SerializeField] private Camera levelSelectCamera;
        [SerializeField] private GameObject levelSelectCollection;

        public UnityEvent<string> levelName;
        private static string _levelCompletionKey = "BoidGolfLevelCompletion";

        private AsyncFnOnceCell _levelLoadCell;

        private void Awake()
        {
            _levelLoadCell = new AsyncFnOnceCell(gameObject);
            
            DontDestroyOnLoad(this);
            if(SingletonLocator<LevelManager>.Instance != this)
            {
                Destroy(this);
            }
        }

        private void Start()
        {
            _levelLoadCell.TryRun(c => LoadLevelAsync(0, c), "Cannot run");
        }
        
        public void RestartLevel()
        {
            _levelLoadCell.TryRun(RestartLevelAsync, "Cannot run");
        }

        public void NextLevel()
        {
            _levelLoadCell.TryRun(c => LoadLevelAsync(currentLevel + 1, c), "Cannot run");
        }

        public void CompleteLevel(int levelIndexId, LevelCompletionData completionData)
        {
            Debug.Log($"completed level {levelIndexId} with {completionData.usedObstacles} obstacles");

            var existingSaveData = GetExistingSaveData();
            
            if (existingSaveData.levelData.Count <= levelIndexId)
            {
                existingSaveData.levelData.AddRange(Enumerable.Repeat(
                    LevelSaveData.Empty, levelIndexId - existingSaveData.levelData.Count + 1));
            }
            var levelSaveData = existingSaveData.levelData[levelIndexId];
            if (levelSaveData.best < 0 || completionData.usedObstacles < levelSaveData.best)
            {
                Debug.Log($"new best score for level {levelIndexId}: {completionData.usedObstacles} obstacles");
                levelSaveData.best = completionData.usedObstacles;
            }

            
            
            SimpleSave.Set(_levelCompletionKey, existingSaveData);
            SimpleSave.Save();
        }

        private LevelsSaveData GetExistingSaveData()
        {
            SimpleSave.Refresh();
            return SimpleSave.Get(_levelCompletionKey, LevelsSaveData.Empty);
        }
        
        public void ExitLevel()
        {
            _levelLoadCell.TryRun(ExitCurrentLevelAsync, "Cannot run");
        }

        public void LoadLevelByIndex(int levelIndexId)
        {
            _levelLoadCell.TryRun(c => LoadLevelAsync(levelIndexId, c), "Cannot run");
        }

        public IEnumerable<LevelData> GetLevelData()
        {
            var existingSaveData = GetExistingSaveData();
            return levels.Select((_, index) => CreateLevelData(index, existingSaveData));
        }
        
        private LevelData CreateLevelData(int levelIndex, LevelsSaveData existingSaveData)
        {
            return new LevelData
            {
                LevelIndexId = levelIndex,
                SetupData = levels[levelIndex].metadata,
                SaveData = existingSaveData.levelData.Count > levelIndex ? 
                    existingSaveData.levelData[levelIndex] : LevelSaveData.Empty
            };
        }

        private async UniTask ExitCurrentLevelAsync(CancellationToken c)
        {
            await UnloadAsync(currentLevel, c);
            levelSelectCollection.SetActive(true);
        }
        
        private async UniTask LoadLevelAsync(int levelIndex, CancellationToken cancel)
        {
            if (levelIndex < 0 || levelIndex >= levels.Length)
            {
                Log.Error($"invalid level index: {levelIndex}");
                return;
            }
            
            levelSelectCollection.SetActive(false);
            
            levelName.Invoke(levels[levelIndex].metadata.levelName);

            await UnloadAsync(currentLevel, cancel);
            await LoadAsync(levelIndex, cancel);
            currentLevel = levelIndex;
        }

        private async UniTask LoadAsync(int levelIndex, CancellationToken cancel)
        {
            var level = levels[levelIndex];
            
            Debug.Log($"loading level: {level.metadata.levelName}");
            await SceneManager
                .LoadSceneAsync(level.sceneReference.scenePath, LoadSceneMode.Additive)
                .WithCancellation(cancel);
            
            var inLevelActionComponent = SceneManager.GetSceneByBuildIndex(level.sceneReference.BuildIndex)
                .GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<InLevelActions>())
                .FirstOrDefault();
            if (inLevelActionComponent != null)
            {
                var existingSaveData = GetExistingSaveData();
                var levelData = CreateLevelData(levelIndex, existingSaveData);
                inLevelActionComponent.InitializeWith(levelData);
            }
            else
            {
                Debug.LogWarning($"No InLevelActions component found in scene {level.sceneReference.Name}");
            }
            levelSelectCamera.gameObject.SetActive(false);
        }
        
        private async UniTask UnloadAsync(int levelIndex, CancellationToken cancel)
        {
            var level = levels[levelIndex];
            
            Debug.Log($"unloading level: {level.metadata.levelName}");
            var loadedScene = level.sceneReference.ScenePointerIfLoaded;
            if (loadedScene.IsValid())
            {
                await SceneManager.UnloadSceneAsync(loadedScene)
                    .WithCancellation(cancel);
            }
            levelSelectCamera.gameObject.SetActive(true);
        }
        private async UniTask RestartLevelAsync(CancellationToken c)
        {
            await UnloadAsync(currentLevel, c);
            await UniTask.NextFrame(c);
            await LoadAsync(currentLevel, c);
        }
        
    }
}