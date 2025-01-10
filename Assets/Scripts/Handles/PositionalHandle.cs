using System;
using UnityEditor;
using UnityEngine;

namespace Handles
{
    public class PositionalHandle : MonoBehaviour
    {
        public float radius = 3f;
        public Color color = Color.white;

        private void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(this.transform.position, radius);
        }
    }
    
    #if UNITY_EDITOR
    // A tiny custom editor for ExampleScript component
    [CustomEditor(typeof(PositionalHandle))]
    public class ExampleEditor : Editor
    {
        // Custom in-scene UI for when ExampleScript
        // component is selected.
        public void OnSceneGUI()
        {
            //DrawHandles();
        }
        
        private void OnScene(SceneView sceneview)
        {
        }

        private void DrawHandles()
        {
            var t = target as PositionalHandle;
            var tr = t.transform;
            var pos = tr.position;
            var scale = tr.localScale;
            // display an orange disc where the object is
            var color = new Color(1, 0.8f, 0.4f, 1);
            UnityEditor.Handles.color = color;
            
            UnityEditor.Handles.TransformHandle(ref pos, tr.rotation, ref scale);
        }
    	   //
        // void OnEnable()
        // {
        //     SceneView.duringSceneGui -= OnScene;
        //     SceneView.duringSceneGui += OnScene;
        // }
    }
    #endif
}