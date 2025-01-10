using System;
using UnityEngine;

namespace Other
{
    public class SetTimeScale : MonoBehaviour
    {
        public float stepMultiple = 0.1f;

        public void SetScale(float timeScale)
        {
            Time.timeScale = timeScale * stepMultiple;
        }

        private void OnEnable()
        {
            Time.timeScale = 1;
        }

        private void OnDisable()
        {
            Time.timeScale = 1;
        }
    }
}