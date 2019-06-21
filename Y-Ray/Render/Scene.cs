using System;
using System.Collections.Generic;
using VecMath;
using VecMath.Geometry;
using YRay.Render.Object;

namespace YRay.Render
{
    public class Scene
    {
        public Camera Camera { get; }

        public List<IObject> Objects { get; } = new List<IObject>();

        public List<Light> Lights { get; } = new List<Light>();

        public Vector3 Raytrace(Ray ray, int depth)
        {
            if (CollidingTriangle(ray, 1E+10F, out Triangle tri, out Vector3 pos, out int mat))
            {
                Vector3 color = Vector3.Zero;

                foreach (var light in Lights)
                {
                    var vec = light.Pos - pos;

                    color += RaytraceToLight(light, pos, depth) * Math.Abs(MathUtil.Dot(vec, tri.Normal));
                }

                for (int i = 0; i < 10; i++)
                {
                    var randomvec = Vector3.Zero;

                    color += Raytrace(new Ray(ray.pos, randomvec), depth + 1) * Math.Abs(MathUtil.Dot(randomvec, tri.Normal));
                }

                return color * (1.0F / (Lights.Count + 10));
            }

            return Vector3.Zero;
        }

        public Vector3 RaytraceToLight(Light light, Vector3 pos, int depth)
        {
            var vec = light.Pos - pos;
            float distance = vec.Length();
            var ray = new Ray(pos, vec * (1 / distance));

            if (CollidingTriangle(ray, distance, out Triangle tri, out Vector3 nextPos, out int nextMat))
            {
                return RaytraceToLight(light, nextPos, depth + 1);
            }

            return light.Color * (1 / (distance * distance));
        }

        bool CollidingTriangle(Ray ray, float minDistance, out Triangle tri, out Vector3 pos, out int material)
        {
            float min = minDistance;
            float max = 0;

            var candicates = new List<(IObject obj, float min)>();

            foreach (var obj in Objects)
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
            }

            tri = new Triangle();
            pos = Vector3.Zero;
            material = 0;

            if (candicates.Count == 0) return false;

            bool result = false;

            foreach ((IObject obj, float start) in candicates)
            {
                if (obj.DistanceToRay(ray, min, out var tri_temp, out float dis, out int mat))
                {
                    min = dis;
                    tri = tri_temp;
                    material = mat;
                    result = true;
                }
            }
            pos = ray.pos + ray.vec * min;

            return result;
        }
    }
}
