using System;
using Microsoft.Xna.Framework;
using Rune.MonoGame;

namespace Editor.Geometry
{
    public class Aabb
    {
        private Vector3 _min;
        private Vector3 _max;
        
        public Vector3 Min => _min;
        public Vector3 Max => _max;
        public Vector3 Center => (_max + _min) * 0.5f;
        public bool IsValid => _min.X <= _max.X && _min.Y <= _max.Y && _min.Z <= _max.Z; 

        public Aabb()
        {
            Invalidate();
        }
        
        /// <summary>
        /// Invalidates aabb
        /// </summary>
        public void Invalidate()
        {
            _min.X = float.MaxValue;
            _min.Y = float.MaxValue;
            _min.Z = float.MaxValue;
            _max.X = float.MinValue;
            _max.Y = float.MinValue;
            _max.Z = float.MinValue;
        }

        /// <summary>
        /// Grows the bounding box by the given vector
        /// </summary>
        /// <param name="vector"></param>
        public void Grow(Vector3 vector)
        {
            Grow(vector.X, vector.Y, vector.Z);
        }

        /// <summary>
        /// Grows the bounding box by the given vector
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Grow(float x, float y, float z)
        {
            if (x < _min.X) _min.X = x;
            if (y < _min.Y) _min.Y = y;
            if (z < _min.Z) _min.Z = z;
            if (x > _max.X) _max.X = x;
            if (y > _max.Y) _max.Y = y;
            if (z > _max.Z) _max.Z = z;
        }

        /// <summary>
        /// If the box has area in any of the axis it has 2D volume
        /// </summary>
        /// <returns>return true if it has 2D volume</returns>
        public bool HasArea() {
            float absX = Math.Abs(_max.X - _min.X);
            float absY = Math.Abs(_max.Y - _min.Y);
            float absZ = Math.Abs(_max.Z - _min.Z);

            return absX > Maths.EpsilonSmall && absY > Maths.EpsilonSmall ||
                   absX > Maths.EpsilonSmall && absZ > Maths.EpsilonSmall ||
                   absY > Maths.EpsilonSmall && absZ > Maths.EpsilonSmall;
        }

        /// <summary>
        /// If the box has volume in all of the axis it has 3D volume
        /// </summary>
        /// <returns>true if it has 3D volume</returns>
        public bool HasVolume() {
            float absX = Math.Abs(_max.X - _min.X);
            float absY = Math.Abs(_max.Y - _min.Y);
            float absZ = Math.Abs(_max.Z - _min.Z);

            return absX > Maths.EpsilonSmall && absY > Maths.EpsilonSmall && absZ > Maths.EpsilonSmall;
        }
    }
}