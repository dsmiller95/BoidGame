using UnityEngine;

namespace Other
{
    [ExecuteAlways]
    public class RainbowText : MonoBehaviour
    {
        public TMPro.TMP_Text text;
        public float speed = 1;
        public float saturation = 1;
        public float value = 1;

        [SerializeField] private bool inEditMode;
        
        private void Update()
        {
            if (!Application.isPlaying && !inEditMode) return;
            
            var time = Time.time;
            var hue = time * speed;
            var color = Color.HSVToRGB(time % 1, saturation, value);
            text.color = color;
        }
    }
}