using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Graphics
{
    public class Camera
    {
        private Quaternion _orientation = Quaternion.Identity;
        private Vector3 _translation = Vector3.Zero;
        private bool _isDirty;

        private Matrix _view;
        private Matrix _projection;

        private float _fov;
        private float _aspect;
        private float _near;
        private float _far;
        
        public float Fov => _fov;

        public float Aspect => _aspect;

        public float Near => _near;

        public float Far => _far;

        public Matrix Projection => _projection;

        public Vector3 Position
        {
            get => _translation;
            set
            {
                _isDirty = true;
                _translation = value;
            }
        }

        public Matrix View
        {
            get
            {
                if (!_isDirty)
                    return _view;

                _view = Matrix.Invert(Matrix.CreateFromQuaternion(_orientation) * Matrix.CreateTranslation(_translation));
                return _view;
            }
        }

        public Camera()
        {
            _projection = Matrix.Identity;
        }

        public Camera(int width, int height, float near, float far)
        {
            _near = near;
            _far = far;
            _projection = Matrix.CreateOrthographic(width, height, near, far);
        }

        public Camera(float fov, float aspect, float near, float far)
        {
            _fov = fov;
            _aspect = aspect;
            _near = near;
            _far = far;
            _projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov), aspect, near, far);
        }

        public Vector3 ScreenToWorld(Viewport viewport, Vector2 mousePosition, bool near = true)
        {
            return viewport.Unproject(new Vector3(mousePosition, near ? 0 : 1), Projection,
                View, Matrix.Identity);
        }

        public Vector3 WorldToScreen(Viewport viewport, Vector3 worldPosition)
        {
            return viewport.Project(worldPosition, Projection, View, Matrix.Identity);
        }

        public void Move(Vector3 movement)
        {
            _translation.X += movement.X;
            _translation.Y += movement.Y;
            _translation.Z += movement.Z;
            _isDirty = true;
        }

        public void MoveLocal(Vector3 movement)
        {
            _translation += Vector3.Transform(movement, _orientation);
            _isDirty = true;
        }

        public void Rotate(Vector3 axis, float angle)
        {
            var radians = MathHelper.ToRadians(angle);
            _orientation = Quaternion.CreateFromAxisAngle(axis, radians) * _orientation;
            _orientation.Normalize();
            _isDirty = true;
        }

        public void RotateLocal(Vector3 axis, float angle)
        {
            var radians = MathHelper.ToRadians(angle);
            _orientation = _orientation * Quaternion.CreateFromAxisAngle(axis, radians);
            _orientation.Normalize();
            _isDirty = true;
        }
    }
}