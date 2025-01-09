using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    public class FullScreenSDFRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class SDFSettings
        {
            public Material sdfMaterial = null;
            public RenderPassEvent passEvent = RenderPassEvent.AfterRendering;
        }
        
        [SerializeField] private SDFSettings settings;
    
        private FullScreenSDFPass _sdfPass;

        public override void Create()
        {
            if(settings.sdfMaterial != null)
            {
                _sdfPass = new FullScreenSDFPass(
                    "FullScreenSDFPass",
                    settings.passEvent,
                    settings.sdfMaterial
                );
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (_sdfPass == null || settings.sdfMaterial == null) return;
            
            if (renderingData.cameraData.cameraType is CameraType.Game or CameraType.SceneView)
            {
                _sdfPass.ConfigureInput(ScriptableRenderPassInput.Color);
                //_sdfPass.SetTarget(renderer.cameraColorTargetHandle, m_Intensity);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_sdfPass == null || settings.sdfMaterial == null) return;
            
            if (renderingData.cameraData.cameraType is CameraType.Game or CameraType.SceneView)
            {
                renderer.EnqueuePass(_sdfPass);
            }
        }
        
    }
}