using Unity.Mathematics;

namespace Boids.Domain.Zones
{
    public static class ZoneUtils
    {
        public static bool Contains(this in Zone zone, float2 worldPoint)
        {
            return RectContains(zone.MinWorld, zone.MaxWorld, worldPoint);
        }
        
        public static bool RectContains(in float2 min, in float2 max, in float2 point)
        {
            return point.x >= min.x && point.x <= max.x &&
                   point.y >= min.y && point.y <= max.y;
        }
        
    }
}