using System;
using Microsoft.Xna.Framework;

namespace Rune.MonoGame
{
    public struct Vector2i
    {
        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct Vector3i
    {
        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public static class Maths
    {
        public static float EpsilonBig = 0.001f;
        public static float EpsilonSmall = 0.000001f;

        public static void RaycastImplicitGrid(Vector3 start, Vector3 end,
            int cellSize, Func<Vector3i, Boolean> visit)
        {
            Vector3i tileCoords = new Vector3i
            {
                X = (int) Math.Floor(start.X / cellSize),
                Y = (int) Math.Floor(start.Y / cellSize),
                Z = (int) Math.Floor(start.Z / cellSize)
            };

            int endX = (int) Math.Floor(end.X / cellSize);
            int endY = (int) Math.Floor(end.Y / cellSize);
            int endZ = (int) Math.Floor(end.Z / cellSize);

            int totalTiles = 1 + Math.Abs(endX - tileCoords.X) + Math.Abs(endY - tileCoords.Y) +
                             Math.Abs(endZ - tileCoords.Z);

            int incX = Math.Sign(end.X - start.X);
            int incY = Math.Sign(end.Y - start.Y);
            int incZ = Math.Sign(end.Z - start.Z);

            float absDeltaX = Math.Abs(end.X - start.X);
            float absDeltaY = Math.Abs(end.Y - start.Y);
            float absDeltaZ = Math.Abs(end.Z - start.Z);

            float deltaX = cellSize / absDeltaX;
            float deltaY = cellSize / absDeltaY;
            float deltaZ = cellSize / absDeltaZ;

            float minX = cellSize * tileCoords.X;
            float maXX = minX + cellSize;

            float minY = cellSize * tileCoords.Y;
            float maXY = minY + cellSize;

            float minZ = cellSize * tileCoords.Z;
            float maXZ = minZ + cellSize;

            float tX = (start.X > end.X ? start.X - minX : maXX - start.X) / absDeltaX;
            float tY = (start.Y > end.Y ? start.Y - minY : maXY - start.Y) / absDeltaY;
            float tZ = (start.Z > end.Z ? start.Z - minZ : maXZ - start.Z) / absDeltaZ;

            while (totalTiles > 0)
            {
                if (visit(tileCoords))
                    break;

                totalTiles--;

                if (tX <= tY && tX <= tZ)
                {
                    tX += deltaX;
                    tileCoords.X += incX;
                }
                else if (tY <= tX && tY <= tZ)
                {
                    tY += deltaY;
                    tileCoords.Y += incY;
                }
                else
                {
                    tZ += deltaZ;
                    tileCoords.Z += incZ;
                }
            }
        }

        public static bool RayIntersectsBox(Vector3 position, Vector3 direction, Vector3 min, Vector3 max,
            float expand, ref Vector2 tVector)
        {
            float reciprocalX = 1.0f / direction.X;
            float reciprocalY = 1.0f / direction.Y;
            float reciprocalZ = 1.0f / direction.Z;

            float t1 = (min.X - expand - position.X) * reciprocalX;
            float t2 = (max.X + expand - position.X) * reciprocalX;
            float t3 = (min.Y - expand - position.Y) * reciprocalY;
            float t4 = (max.Y + expand - position.Y) * reciprocalY;
            float t5 = (min.Z - expand - position.Z) * reciprocalZ;
            float t6 = (max.Z + expand - position.Z) * reciprocalZ;

            float tMin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            float tMax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            if (tMax < 0 || tMin > tMax)
                return false;

            tVector.X = tMin;
            tVector.Y = tMax;

            return true;
        }

        public static bool RayIntersectsPlane(Vector3 normal, float distance, Vector3 position, Vector3 direction,
            ref Vector3 worldPosition)
        {
            float directionDot = Vector3.Dot(normal, direction);
            if (Math.Abs(directionDot) < EpsilonSmall)
                return false;

            var distanceToPlane = Vector3.Dot(normal, position) - distance;
            float t = distanceToPlane / -directionDot;
            if (t < EpsilonSmall)
                return false;

            worldPosition = position + direction * t;

            return true;
        }

        public static Vector3 GetAabbFaceNormalRhs(ref Vector3 min, ref Vector3 max, ref Vector3 hitPosition)
        {
            if (Math.Abs(hitPosition.X - min.X) <= EpsilonBig)
            {
                return Vector3.Left;
            }

            if (Math.Abs(hitPosition.X - max.X) <= EpsilonBig)
            {
                return Vector3.Right;
            }

            if (Math.Abs(hitPosition.Y - min.Y) <= EpsilonBig)
            {
                return Vector3.Down;
            }

            if (Math.Abs(hitPosition.Y - max.Y) <= EpsilonBig)
            {
                return Vector3.Up;
            }

            if (Math.Abs(hitPosition.Z - min.Z) <= EpsilonBig)
            {
                return Vector3.Forward;
            }

            if (Math.Abs(hitPosition.Z - max.Z) <= EpsilonBig)
            {
                return Vector3.Backward;
            }

            return Vector3.Zero;
        }

        public static void GetOrthogonalNormalsRhs(this ref Vector3 normal, out Vector3 horizontal, out Vector3 vertical)
        {
            normal.Normalize();

            Vector3 initial = Vector3.Up;

            float dotProduct = Vector3.Dot(initial, normal);
            if (1 - Math.Abs(dotProduct) < EpsilonSmall)
                initial = Vector3.Forward;

            horizontal = Vector3.Cross(normal, initial);
            vertical = Vector3.Cross(horizontal, normal);
        }

        public static Vector3 Floor(this ref Vector3 vector, float dimension)
        {
            var x = (float) Math.Floor(vector.X * dimension) / dimension;
            var y = (float) Math.Floor(vector.Y * dimension) / dimension;
            var z = (float) Math.Floor(vector.Z * dimension) / dimension;
            return new Vector3(x, y, z);
        }

        public static Vector3 Round(this ref Vector3 vector, float dimension)
        {
            var x = (float) Math.Round(vector.X * dimension) / dimension;
            var y = (float) Math.Round(vector.Y * dimension) / dimension;
            var z = (float) Math.Round(vector.Z * dimension) / dimension;
            return new Vector3(x, y, z);
        }

        public static Vector3 Ceiling(this ref Vector3 vector, float dimension)
        {
            var x = (float) Math.Ceiling(vector.X * dimension) / dimension;
            var y = (float) Math.Ceiling(vector.Y * dimension) / dimension;
            var z = (float) Math.Ceiling(vector.Z * dimension) / dimension;
            return new Vector3(x, y, z);
        }

        public static float DotPerp(this ref Vector2 lhs, ref Vector2 rhs)
        {
            return (-lhs.Y * rhs.X) + (lhs.X * rhs.Y);
        }
    }
}