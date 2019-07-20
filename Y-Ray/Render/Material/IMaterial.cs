using System;
using VecMath;

namespace YRay.Render.Material
{
    public interface IMaterial
    {
        Vector3 Emission { get; set; }

        void Scatter(Ray ray, Vector3 normal, Vector2 uv, Vector3 pos, out Vector3 vec, out Vector3 albedo);

        bool ShouldReflectPhoton();

        IMaterial Copy();
    }

    public class MaterialLambert : IMaterial
    {
        public Vector3 Emission { get; set; }
        public ITexture Albedo { get; set; }
        public float Roughness { get; set; }

        protected static readonly Random Rand = new Random();

        public IMaterial Copy() => new MaterialLambert
        {
            Emission = Emission,
            Albedo = Albedo,
        };

        public virtual void Scatter(Ray ray, Vector3 normal, Vector2 uv, Vector3 pos, out Vector3 vec, out Vector3 albedo)
        {
            float dot = VMath.Dot(normal, ray.vec);

            albedo = Albedo.GetColor(uv) * Math.Abs(dot);

            vec = CreateReflectedVector(ray, normal, pos);
        }

        public bool ShouldReflectPhoton()
        {
            return true;
        }

        protected Vector3 CreateReflectedVector(Ray ray, Vector3 normal, Vector3 pos)
        {
            if (Rand.NextDouble() < Roughness)
            {
                var vec = new Vector3((float)Rand.NextDouble() * 0.5F - 0.25F, 3, (float)Rand.NextDouble() * 0.5F - 0.25F) - pos;
                if (VMath.Dot(vec, normal) > 0) return VMath.Normalize(vec);
            }

            return Vector3.Interpolate(Reflect(ray.vec, normal), RandomCosineDirection() * Matrix3.LookAt(normal, RandomCosineDirection()), Roughness);
        }

        public Vector3 Reflect(Vector3 vec, Vector3 nornal)
        {
            return VMath.Normalize(vec - 2 * VMath.Dot(vec, nornal) * nornal);
        }

        public Vector3 RandomCosineDirection()
        {
            double r1 = Rand.NextDouble();
            double r2 = Rand.NextDouble();
            float z = (float)Math.Sqrt(1 - r2);
            double phi = Math.PI * 2 * r1;
            float x = (float)(Math.Cos(phi) * Math.Sqrt(r2));
            float y = (float)(Math.Sin(phi) * Math.Sqrt(r2));
            return VMath.Normalize(new Vector3(x, y, z));
        }
    }
}
