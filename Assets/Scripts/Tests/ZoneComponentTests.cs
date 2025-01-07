using Boids.Domain.Zones;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tests
{
    public class ZoneComponentTests
    {
        [Test]
        public void GetRelative_WorksWhenTranslateAndScale()
        {
            var transform = new LocalToWorld
            {
                Value = float4x4.TRS(new float3(1, 2, 3), quaternion.identity, new float3(3, 2, 1))
            };
            var zone = new ZoneComponent
            {
                Extents = new float2(1, 2),
            };
        
            var result = zone.GetRelative(transform);
        
            var expected = new Zone
            {
                MinWorld = new float2(-2, -2),
                MaxWorld = new float2(4, 6),
            };
            Assert.AreEqual(expected, result);
            Assert.AreNotEqual(new Zone(), result);
        }
    }
}
