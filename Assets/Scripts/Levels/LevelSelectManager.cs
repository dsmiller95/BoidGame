using System;
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
            
            var levels = SingletonLocator<IManageLevels>.Instance.GetLevelData();
            foreach (var level in levels)
            {
                var newCell = Instantiate(levelSelectCellPrefab, levelSelectCellContainer.transform);
                newCell.InitializeWith(level);
            }
        }
        

        public void LevelClicked(LevelData levelData)
        {
            SingletonLocator<IManageLevels>.Instance.LoadLevelByIndex(levelData.LevelIndexId);
        }
    }
}