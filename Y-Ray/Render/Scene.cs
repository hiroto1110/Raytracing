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

        public Vector3 Raytrace(Ray ray)
        {
            (Triangle tri, Vector3 pos, int matrial) = CollidingTriangle(ray);

            Vector3 color = Vector3.Zero;

            foreach (var light in Lights)
            {

            }

            return color;
        }

        public Vector3 RayTraceToLight(Light light , Ray ray)
        {

        }

        (Triangle tri, Vector3 pos, int matrial) CollidingTriangle(Ray ray)
        {
            float min = 1E+12F;
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

            (Triangle tri, Vector3 pos, int matrial) result = (new Triangle(), Vector3.Zero, 0);

            foreach ((IObject obj, float start) in candicates)
            {
                (float distance, Triangle tri, int material) = obj.DistanceToRay(ray);

                if (min > distance)
                {
                    min = distance;

                    result = (tri, ray.pos + ray.vec * distance, material);
                }
            }
            return result;
        }
    }
}
