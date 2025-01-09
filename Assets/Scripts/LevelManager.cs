using System;
using System.Collections;
using System.Linq;
using Dman.Utilities.Logger;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class LevelManager : MonoBehaviour
    {
        [Serializable]
        private struct Level
        {
            [TextArea]
            public string label;
            // TODO: make work in build
            public SubScene subScene;
        }
        
        [SerializeField] private Level[] levels;
        [SerializeField] private int currentLevel;

        public UnityEvent<string> levelName;
        
        private void Awake()
        {
            foreach (Level level in levels)
            {
                level.subScene.enabled = false;
            }
            
            if (levels.Skip(1).Any(x => x.subScene.enabled))
            {
                
                Log.Error("expected all levels to begin disabled");
            }
        }

        private void Start()
        {
            foreach (var level in levels)
            {
                Debug.Log($"Level: {level.label}");
            }

            LoadLevel(0);
        }
        
        private void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Length)
            {
                Log.Error($"invalid level index: {levelIndex}");
                return;
            }
            
            Debug.Log($"loading level: {levels[levelIndex].label}");
            levelName.Invoke(levels[levelIndex].label);
            
            
            levels[currentLevel].subScene.enabled = false;
            levels[levelIndex].subScene.enabled = true;
            currentLevel = levelIndex;
        }
        
        public void RestartLevel()
        {
            StartCoroutine(RestartLevelCoroutine());
        }

        private IEnumerator RestartLevelCoroutine()
        {
            levels[currentLevel].subScene.enabled = false;
            yield return null;
            LoadLevel(currentLevel);
        }

        public void NextLevel()
        {
            LoadLevel(currentLevel + 1);
        }
    }
}