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
        
        public void InitializeWith(LevelData data, bool unlocked)
        {
            _data = data;
            levelName.text = data.SetupData.levelName;

            var bestScoreNumber = data.SaveData?.best ?? -1;
            var bestScore = bestScoreNumber == -1 ? "N/A" : bestScoreNumber.ToString();
            scoreBox.text = $"Par: {data.SetupData.par}\nBest: {bestScore}";

            playButton.interactable = unlocked;
        }
        
        public void OnClick()
        {
            var levelSelectManager = this.GetComponentInParent<LevelSelectManager>();
            levelSelectManager.LevelClicked(_data);
        }
    }
}