using System;
using VecMath;

namespace YRay.Render.Object
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Matrix3 Rotation { get; set; } = Matrix3.Identity;

        float fov;
        public float Fov
        {
            get => fov;
            set
            {
                fov = value;
                TanFov = (float)Math.Tan(value);
            }
        }
        private float TanFov { get; set; }

        public float Aspect { get; set; }

        public Ray CreateRay(float u, float v)
        {
            var forward = Vector3.UnitZ * Rotation;
            var side = Vector3.UnitX * Rotation;
            var up = Vector3.UnitY * Rotation;

            var width = TanFov;
            var height = Aspect * width;

            var xcomp = side * (u * width);
            var ycomp = up * (v * height);

            var vec = VMath.Normalize(forward + xcomp + ycomp);

            return new Ray(Position, vec);
        }
    }
}
