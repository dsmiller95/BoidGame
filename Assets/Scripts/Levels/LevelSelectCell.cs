using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace Levels
{
    public class LevelSelectCell : MonoBehaviour
    {
        public TMPro.TMP_Text levelName;
        public TMPro.TMP_Text scoreBox;
        public Button playButton;
        
        [CanBeNull] private LevelData _data;
        
        public void InitializeWith(LevelData data, bool forceUnlocked)
        {
            _data = data;
            levelName.text = data.SetupData.levelName;
            
            var bestScore = data.SaveData?.best.ToString() ?? "N/A"; 
            scoreBox.text = $"Par: {data.SetupData.par}\nBest: {bestScore}";

            playButton.interactable = forceUnlocked || (data.SaveData?.best >= 0);
        }
        
        public void OnClick()
        {
            var levelSelectManager = this.GetComponentInParent<LevelSelectManager>();
            levelSelectManager.LevelClicked(_data);
        }
    }
}