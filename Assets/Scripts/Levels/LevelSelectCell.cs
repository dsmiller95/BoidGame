using JetBrains.Annotations;
using UnityEngine;

namespace Levels
{
    public class LevelSelectCell : MonoBehaviour
    {
        public TMPro.TMP_Text levelName;
        public TMPro.TMP_Text scoreBox;
        
        [CanBeNull] private LevelData _data;
        
        public void InitializeWith(LevelData data)
        {
            _data = data;
            levelName.text = data.SetupData.levelName;
            scoreBox.text = data.SaveData?.best.ToString() ?? "Not completed";
        }
        
        public void OnClick()
        {
            var levelSelectManager = this.GetComponentInParent<LevelSelectManager>();
            levelSelectManager.LevelClicked(_data);
        }
    }
}