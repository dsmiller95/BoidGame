using System.Collections.Generic;

namespace Boids.Domain
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereHasValue<T>(this IEnumerable<T?> enumerable)
            where T : struct
        {
            foreach (var item in enumerable)
            {
                if (item.HasValue)
                {
                    yield return item.Value;
                }
            }
        }
    }
}