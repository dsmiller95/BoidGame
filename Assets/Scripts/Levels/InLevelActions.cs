using Dman.Utilities;
using UnityEngine;

namespace Levels
{
    public class InLevelActions : MonoBehaviour
    {
        public TMPro.TMP_Text labelText;

        public void InitializeWith(LevelSetupData levelData)
        {
            labelText.text = levelData.levelName + "\nPar: " + levelData.par;
        }
        
        public void RestartLevel()
        {
            SingletonLocator<IManageLevels>.Instance.RestartLevel();
        }

        public void NextLevel()
        {
            SingletonLocator<IManageLevels>.Instance.NextLevel();
        }
        
        public void ExitLevel()
        {
            SingletonLocator<IManageLevels>.Instance.ExitLevel();
        }
    }
}