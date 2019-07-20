using System;
using System.Collections.Generic;
using System.Linq;
using VecMath;
using YRay.Render.Material;
using YRay.Render.Object;

namespace YRay.Render
{
    public class HitResult
    {
        public Vector3 normal;
        public Vector2 uv;
        public float distance;
        public IMaterial material;
    }

    public class Sun
    {
        public Vector3 Pos { get; set; } = VMath.Normalize(new Vector3(1, 1, -1));

        public Vector3 Color { get; set; } = new Vector3(1, 1, 1) * 2;

        public Vector3 GetColor(Ray ray)
        {
            return VMath.Dot(Pos, ray.vec) > 0.995 ? Color : new Vector3(1, 1, 1) * 0.1F;
        }
    }

    public class Scene
    {
        public Camera Camera { get; } = new Camera();

        public Sun Sun { get; } = new Sun();

        public List<IObject> Objects { get; } = new List<IObject>();

        public int MaxDepth { get; set; } = 6;

        public void InitPreRendering()
        {
            foreach (var obj in Objects)
            {
                obj.InitPreRendering();
            }
        }

        public Vector3 Raytrace(float u, float v)
        {
            return Raytrace(Camera.CreateRay(u, v), 0);
        }

        public Vector3 Raytrace(Ray ray, int depth)
        {
            var result = new HitResult() { distance = 1E+10F };

            if (CollidingPolygon(ray, result))
            {
                if (depth > MaxDepth)
                {
                    return new Vector3(1, 1, 1) * 0.1;
                }

                var pos = ray.pos + ray.vec * result.distance;

                Vector3 color = result.material.Emission  /   ((pos - ray.pos).LengthSquare() * 4);

                if (result.material.Emission == Vector3.Zero)
                {
                    int sample;
                    switch (depth)
                    {
                        case 0: sample = 1; break;
                        case 1: sample = 1; break;
                        default: sample = 1; break;
                    }

                    for (int i = 0; i < sample; i++)
                    {
                        if (depth == 0 && color >= new Vector3(1, 1, 1)) break;

                        result.material.Scatter(ray, result.normal, result.uv, pos, out Vector3 nextVec, out Vector3 alobedo);

                        color += Vector3.Scale(alobedo, Raytrace(new Ray(pos, nextVec), depth + 1)) / sample;
                    }
                }
                return color;
            }
            return Vector3.Zero;
        }

        bool CollidingPolygon(Ray ray, HitResult result)
        {
            var candicates = new List<(IObject obj, float start)>();

            foreach (var obj in Objects)
            {
                  lock (obj)
                if (obj.AABB.CalculateTimeToIntersectWithRay(Ray.Transform(ray, obj.TransformInv), out float start, out float end))
                    {
                        if (end <= 0) continue;

                        if (result.distance > start)
                        {
                            candicates.Add((obj, start));
                        }
                    }
            }

            if (candicates.Count == 0) return false;

            bool hit = false;

            foreach ((IObject obj, float start) in candicates.OrderBy(g => g.start))
            {
                lock (obj)
                    if (start < result.distance && obj.DistanceToRay(ray, result))
                    {
                        hit = true;
                    }
            }

            return hit;
        }
    }
}
