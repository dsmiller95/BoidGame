using System;
using UnityEngine;

namespace Boids.Domain.OnClick
{
    public class OnClickMonoTest : MonoBehaviour
    {
        private void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Debug.Log("Mouse clicked");
            }
        }
    }
}