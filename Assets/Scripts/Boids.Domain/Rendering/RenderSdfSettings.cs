using System;
using Dman.Utilities.Logger;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    [Serializable]
    public class RenderSdfSettings
    {
        public Material? sdfMaterial;
        
        public void SetSdfObjects(ref GraphicsBuffer? buffer, int count)
        {
            if (sdfMaterial == null)
            {
                Log.Error("SDF Material is null");
                return;
            }

            sdfMaterial.SetInt("_SDFObjectCount", count);
            if (count > 0)
            {
                if (buffer == null || buffer.count < count)
                {
                    buffer = CreateBuffer(count);
                }
                sdfMaterial.SetBuffer("_SDFObjects", buffer);
            }
        }

        private GraphicsBuffer CreateBuffer(int count)
        {
            return new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                count,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(SDFObjectData))
            );
        }
        
    }
}