using UnityEngine;

namespace Rendering
{
    public class SDFSetupURP : MonoBehaviour
    {
        [System.Serializable]
        public struct SDFObjectData
        {
            public int shapeType;
            public Color color;
            public float radius;
            public Vector2 center;
        }

        public SDFObjectData[] objects;
        public Material sdfMaterial;
    
        private GraphicsBuffer _graphicsBuffer;

        void Start()
        {
            _graphicsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                objects.Length,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(SDFObjectData))
            );
            _graphicsBuffer.SetData(objects);

            sdfMaterial.SetBuffer("_SDFObjects", _graphicsBuffer);
            sdfMaterial.SetInt("_SDFObjectCount", objects.Length);
        }

        void OnDestroy()
        {
            if(_graphicsBuffer != null)
                _graphicsBuffer.Release();
        }
    }
}