using System;
using System.Collections.Generic;
using System.Linq;
using VecMath;
using YRay.Render.Object;
using YRay.Render.Material;

namespace YRay.Render
{
    public class Scene
    {
        public Camera Camera { get; } = new Camera();

        public List<IObject> Objects { get; } = new List<IObject>();

        public int MaxDepth { get; set; } = 10;

        public Vector3 Raytrace(float u, float v)
        {
            return Raytrace(Camera.CreateRay(u, v), 0);
        }

        public Vector3 Raytrace(Ray ray, int depth)
        {
            if (CollidingPolygon(ray, 1E+10F, out Vector3 normal, out Vector2 uv, out Vector3 pos, out IMaterial mat))
            {
                if (depth > MaxDepth)
                {
                    return new Vector3(1, 1, 1) * 0.1;
                }

                if (mat.Emission != Vector3.Zero)
                {
                    return mat.Emission / (depth == 0 ? 1 : Math.Max(2F, (pos - ray.pos).LengthSquare() * 2));
                }

                Vector3 color = Vector3.Zero;

                int sample;
                switch (depth)
                {
                    case 0: sample = 20; break;
                    case 1: sample = 5; break;
                    default: sample = 1; break;
                }

                for (int i = 0; i < sample; i++)
                {
                    if (depth == 0 && color >= new Vector3(1, 1, 1)) break;

                    mat.Scatter(ray, normal, uv, pos, out Vector3 nextVec, out Vector3 alobedo);

                    color += Vector3.Scale(alobedo, Raytrace(new Ray(pos, nextVec), depth + 1)) / sample;
                }

                return color;
            }
            return Vector3.Zero;
        }

        bool CollidingPolygon(Ray ray, float minDistance, out Vector3 normal, out Vector2 uv, out Vector3 pos, out IMaterial material)
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
                        candicates.Add((obj, end));
                    }
                }
            }*/

            candicates = Objects.Select(o => (o, 1e+6f)).ToList();

            normal = Vector3.Zero;
            uv = Vector2.Zero;
            pos = Vector3.Zero;
            material = null;

            if (candicates.Count == 0) return false;

            bool result = false;

            foreach ((IObject obj, float start) in candicates)
            {
                lock (obj)
                {
                    if (obj.DistanceToRay(ray, min, out Vector3 normal_temp, out Vector2 uv_temp, out float dis, out IMaterial mat))
                    {
                        min = dis;
                        normal = normal_temp;
                        uv = uv_temp;
                        material = mat;
                        result = true;
                    }
                }
            }
            pos = ray.pos + ray.vec * min;

            return result;
        }
    }
}
