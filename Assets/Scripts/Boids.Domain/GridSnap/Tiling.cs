using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.GridSnap
{
    /*
     * ChatGPT generated. prompts:
     *
     * write me a function that will take in a point in world space, and a triangle side length. Return the centerpoint of the triangle which contains that point in world space, where the triangles are arranged to tile the infinite plane.
     * write it in C# with Unity's types and vector math
     * could you give me the same thing, but, now for centerpoints of a hexagon?
     * 
     */
    public static class Tiling
    {
        /// <summary>
        /// Returns the centroid of the equilateral triangle in an infinite tiling
        /// of side length `side` that contains the given point `worldPoint`.
        /// </summary>
        /// <param name="worldPoint">The point in world coordinates.</param>
        /// <param name="side">The side length of the equilateral triangles.</param>
        /// <returns>The centroid of the triangle containing `worldPoint`.</returns>
        public static Vector2 FindTriangleCenter(Vector2 worldPoint, float side)
        {
            // 1) Define the two basis vectors (spanning a fundamental parallelogram).
            //    These correspond to one horizontal side (v1) and one side at 60 degrees (v2).
            Vector2 v1 = new Vector2(side, 0f);
            Vector2 v2 = new Vector2(side * 0.5f, Mathf.Sqrt(3f) * side * 0.5f);

            // 2) Compute the coordinates (A, B) of `worldPoint` in the basis (v1, v2).
            //    We need to solve:
            //
            //        worldPoint = A * v1 + B * v2
            //
            //    That is (A, B) = inv(M) * (worldPoint), where
            //
            //        M = | v1.x  v2.x |
            //            | v1.y  v2.y |
            //
            float det = v1.x * v2.y - v1.y * v2.x;
            // (det should be > 0 if side > 0)

            // Inverse of 2x2 matrix:
            //   inv(M) = (1/det) * |  v2.y  -v2.x |
            //                      | -v1.y   v1.x |
            //
            // So,
            //   A = (1/det) * (  v2.y * x + (-v2.x) * y )
            //   B = (1/det) * ( -v1.y * x +   v1.x  * y )
            float A = ( v2.y * worldPoint.x - v2.x * worldPoint.y) / det;
            float B = (-v1.y * worldPoint.x + v1.x  * worldPoint.y) / det;

            // 3) Extract integer parts (which "tile" we are in) and fractional parts (where within that tile).
            int i = Mathf.FloorToInt(A);
            int j = Mathf.FloorToInt(B);

            float alpha = A - i;  // fractional part of A
            float beta  = B - j;  // fractional part of B

            // 4) Determine which of the two triangles within the parallelogram
            //    the point falls into by checking alpha + beta.
            //    Lower triangle T1: (alpha + beta <= 1)
            //    Upper triangle T2: (alpha + beta > 1)
            Vector2 centroidLocal;
            if (alpha + beta <= 1f)
            {
                // Lower triangle T1, centroid = (v1 + v2) / 3
                centroidLocal = (v1 + v2) / 3f;
            }
            else
            {
                // Upper triangle T2, centroid = 2 * (v1 + v2) / 3
                centroidLocal = 2f * (v1 + v2) / 3f;
            }

            // 5) Add back the integer shifts to get the world-space position.
            //    shift = i*v1 + j*v2
            Vector2 shift = i * v1 + j * v2;

            // Final centroid in world space
            Vector2 centroid = shift + centroidLocal;
            return centroid;
        }
        
        /// <summary>
        /// Converts a world coordinate (px, py) to *fractional* axial hex coordinates (q', r'),
        /// assuming a pointy-topped hex layout with side length `side`.
        /// </summary>
        public static Vector2 WorldToAxial(Vector2 worldPoint, float side)
        {
            float px = worldPoint.x;
            float py = worldPoint.y;

            // For pointy-topped layout, from standard references:
            //   q' = (sqrt(3)/3 * px - 1/3 * py) / side
            //   r' = (2/3 * py) / side
            float qPrime = (Mathf.Sqrt(3f) / 3f * px - 1f/3f * py) / side;
            float rPrime = (2f/3f * py) / side;

            return new Vector2(qPrime, rPrime);
        }

        /// <summary>
        /// Rounds fractional axial coords (q', r') to the nearest *integer* axial coords (q, r)
        /// using the standard "cube rounding" approach.
        /// </summary>
        public static Vector2Int RoundAxialToNearestHex(Vector2 axialCoords)
        {
            // We'll treat the axial coords as (q', r'), and for rounding we convert to cube coords: (x, y, z)
            //   x = q'
            //   z = r'
            //   y = -x - z
            float x = axialCoords.x;
            float z = axialCoords.y;
            float y = -x - z;

            // Round each component
            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);

            // We need to ensure x + y + z = 0 exactly in integer space.
            // So we pick which of the three has the largest rounding error and fix it.
            float dx = Mathf.Abs(rx - x);
            float dy = Mathf.Abs(ry - y);
            float dz = Mathf.Abs(rz - z);

            if (dx > dy && dx > dz)
            {
                rx = -ry - rz;
            }
            else if (dy > dz)
            {
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }

            // Now map back to axial:
            //   q = x, r = z   (commonly used convention)
            return new Vector2Int(rx, rz);
        }

        /// <summary>
        /// Converts integer axial coordinates (q, r) back to a world position (x, y),
        /// given the side length `side`.
        /// </summary>
        public static Vector2 AxialToWorld(int q, int r, float side)
        {
            // For pointy-top:
            //   x = s * sqrt(3) * (q + r/2)
            //   y = s * 3/2 * r
            float x = side * Mathf.Sqrt(3f) * (q + r / 2f);
            float y = side * (3f / 2f) * r;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Given a point in world space (px,py) and the hex side length,
        /// find the center of the hex cell (in an infinite pointy-topped tiling)
        /// that contains (or is nearest to) that point.
        /// </summary>
        public static Vector2 FindHexCenter(Vector2 worldPoint, float spacing)
        {
            var inRadius = spacing / 2;
            var side = inRadius * (2 / math.sqrt(3));
            
            // 1) Convert to fractional axial coords (q', r').
            Vector2 axialFloat = WorldToAxial(worldPoint, side);

            // 2) Round to nearest integer axial coords (q, r).
            Vector2Int axialHex = RoundAxialToNearestHex(axialFloat);
            int q = axialHex.x;
            int r = axialHex.y;

            // 3) Convert (q, r) back to world space to get the center.
            Vector2 center = AxialToWorld(q, r, side);

            return center;
        }
    }
}
