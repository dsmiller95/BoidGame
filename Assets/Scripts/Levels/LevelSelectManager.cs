using System;
using System.Linq;
using Dman.Utilities;
using UnityEngine;

namespace Levels
{
    public class LevelSelectManager : MonoBehaviour
    {
        public LevelSelectCell levelSelectCellPrefab;
        public GameObject levelSelectCellContainer;
        
        private void Start()
        {
            UpdateLevelDisplay();
        }

        public void UpdateLevelDisplay()
        {
            foreach (Transform child in levelSelectCellContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            var levels = SingletonLocator<IManageLevels>.Instance.GetLevelData()
                .ToList();

            for (var index = 0; index < levels.Count; index++)
            {
                LevelData level = levels[index];
                var newCell = Instantiate(levelSelectCellPrefab, levelSelectCellContainer.transform);
                
                var isPreviousLevelCompleted = index == 0 || levels[index - 1].SaveData?.best >= 0;
                newCell.InitializeWith(level, unlocked: isPreviousLevelCompleted);
            }
        }
        

        public void LevelClicked(LevelData levelData)
        {
            SingletonLocator<IManageLevels>.Instance.LoadLevelByIndex(levelData.LevelIndexId);
        }
    }
}