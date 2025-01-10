using UnityEngine;

namespace Boids.Domain.Rendering
{
    
    
    public class RenderSdfSettingsSingleton
    {
        public static RenderSdfSettings Instance => _singleton ??= GetSingleton();
        private static RenderSdfSettings? _singleton;
        
        
        private static RenderSdfSettings GetSingleton()
        {
            var materialList = Resources.LoadAll<Material>("RenderSdfMaterial");
            if(materialList.Length == 0)
            {
                Debug.LogWarning("No RenderSdfMaterial found in Resources");
                return new();
            }
            if (materialList.Length != 1)
            {
                Debug.LogWarning("The number of sdf materials object should be 1 or less: " + materialList.Length);
            }

            return new RenderSdfSettings
            {
                sdfMaterial = materialList[0]
            };
        }
        
    }
}