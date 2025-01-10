using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitGameAction : MonoBehaviour
{
    public void ExitGame()
    {
#if UNITY_EDITOR
        if(Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        }
#endif
        Application.Quit();
    }
}
