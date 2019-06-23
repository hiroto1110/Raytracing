using System;
using System.Collections.Generic;
using System.Linq;
using VecMath;
using VecMath.Geometry;
using YRay.Render.Object;

namespace YRay.Render
{
    public class Scene
    {
        public Camera Camera { get; } = new Camera();

        public List<IObject> Objects { get; } = new List<IObject>();

        public int MaxDepth { get; set; } = 0;

        public Random Rand { get; } = new Random();

        public Vector3 Raytrace(float u, float v)
        {
            return Raytrace(Camera.CreateRay(u, v), 0);
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

        public Vector3 Raytrace(Ray ray, int depth)
        {
            if (CollidingPolygon(ray, 1E+10F, out Polygon poly, out Vector3 pos, out Material mat))
            {
                Vector3 color = mat.Emission;

                if (depth > MaxDepth)
                {
                    return color;
                }

                int sample = 10;
                var matrix = Matrix3.LookAt(poly.normal, RandomCosineDirection());

                for (int i = 0; i < sample; i++)
                {
                    var randomvec = RandomCosineDirection() * matrix;
                    color += Raytrace(new Ray(pos, randomvec), depth + 1) * Math.Abs(VMath.Dot(randomvec, poly.normal)) / sample;
                }
                return color;
            }
            return Vector3.Zero;
        }

        bool CollidingPolygon(Ray ray, float minDistance, out Polygon poly, out Vector3 pos, out Material material)
        {
            float min = minDistance;
            float max = 0;

            var candicates = new List<(IObject obj, float min)>();

            /*foreach (var obj in Objects)
            {
                if (obj.AABB.CalculateTimeToIntersect(ray.pos, ray.vec, out float start, out float end))
                {
                    if (end <= 0) continue;

                    if (min > start)
                    {
                        min = start;
                        max = end;

                        candicates.RemoveAll(o => o.min > max);
                    }

                    if (max > start)
                    {
                        candicates.Add((obj, start));
                    }
                }
            }*/

            candicates = Objects.Select(o => (o, 1e+6f)).ToList();

            poly = new Polygon();
            pos = Vector3.Zero;
            material = null;

            if (candicates.Count == 0) return false;

            bool result = false;

            foreach ((IObject obj, float start) in candicates)
            {
                if (obj.DistanceToRay(ray, min, out var poly_temp, out float dis, out Material mat))
                {
                    min = dis;
                    poly = poly_temp;
                    material = mat;
                    result = true;
                }
            }
            pos = ray.pos + ray.vec * min;

            return result;
        }
    }
}
