
using System;
using Boids.Domain.Obstacles;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using Boids.Domain.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Migrations{
    public static class MigrationToolHelper
    {
        public static void UpdateInAll<T>(Func<T, bool> update)
            where T : Component
        {
            // Update prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (string guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var components = prefab.GetComponentsInChildren<T>();
                var anyUpdate = false;
                foreach (var component in components)
                {
                    if (!update(component)) continue;
                    
                    anyUpdate = true;
                    EditorUtility.SetDirty(component);
                }
                
                if (anyUpdate)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            // make sure prefab changes are present before loading scenes
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Update scenes
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            // currently open scenes
            var currentScene = SceneManager.GetActiveScene();
            var currentScenePath = currentScene.path;
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool anyUpdate = false;
                foreach (GameObject rootGameObject in scene.GetRootGameObjects())
                {
                    T[] components = rootGameObject.GetComponentsInChildren<T>(true);
                    foreach (T component in components)
                    {
                        if (!update(component)) continue;
                        anyUpdate = true;
                        EditorUtility.SetDirty(component);
                    }
                }

                if (anyUpdate)
                {
                    EditorSceneManager.SaveScene(scene);
                }
            }
            
            // restore current scene
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/migrate sdf shapes")]
        public static void MigrateTmp()
        {
            UpdateInAll<SdfObjectAuthoring>(SdfObjectAuthoring.Migrate);
        }
    }
}
#endif