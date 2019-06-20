using VecMath;
using VecMath.Geometry;

namespace YRay.Render.Object
{
    public interface IObject
    {
        AABoundingBox AABB { get; }
        (float distance, Triangle tri, int material) DistanceToRay(Ray ray);
    }

    public class Mesh : IObject
    {
        public AABoundingBox AABB { get; }
        public Triangle[] Triangles { get; }
        public Matrix4 Matrix { get; set; }

        public (float distance, Triangle tri, int material) DistanceToRay(Ray ray)
        {
            float min = 1E+10F;
            int idx = -1;

            for (int i = 0; i < Triangles.Length; i++)
            {
                var tri = Triangles[i];
                float time = tri.CalculateTimeToIntersectWithRay(ray);

                if (min > time && time > 0 && tri.IsIntersectWithRay(ray))
                {
                    min = time;
                    idx = i;
                }
            }
            return (min, Triangles[idx], 0);
        }
    }
}
