using Unity.Entities;

namespace Boids.Domain.Obstacles
{
    public static class ObstacleQueryExtensions
    {
        public static EntityQueryBuilder WithEnabledObstacles(this EntityQueryBuilder builder)
        {
            return builder
                .WithNone<ObstacleDisabledFlag, Dragging>();
        }
        public static EntityQueryBuilder WithDisabledObstacles(this EntityQueryBuilder builder)
        {
            return builder
                .WithAny<ObstacleDisabledFlag, Dragging>();
        }
    }
}