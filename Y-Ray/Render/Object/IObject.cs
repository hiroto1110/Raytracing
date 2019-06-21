using VecMath;
using VecMath.Geometry;

namespace YRay.Render.Object
{
    public interface IObject
    {
        AABoundingBox AABB { get; }

        bool DistanceToRay(Ray ray, float minDistance, out Triangle tri, out float distance, out int material);
    }

    public class Mesh : IObject
    {
        public AABoundingBox AABB { get; }
        public Triangle[] Triangles { get; }
        public Matrix4 Matrix { get; set; }

        public bool DistanceToRay(Ray ray, float minDistance, out Triangle tri, out float distance, out int material)
        {
            float min = minDistance;
            int idx = -1;

            tri = new Triangle();
            distance = minDistance;
            material = 0;

            for (int i = 0; i < Triangles.Length; i++)
            {
                var tri_temp = Triangles[i];
                float time = tri_temp.CalculateTimeToIntersectWithRay(ray);

                if (min > time && time > 0 && tri_temp.IsIntersectWithRay(ray))
                {
                    min = time;
                    idx = i;

                    tri = tri_temp;
                    distance = time;
                    material = 0;
                }
            }
            return idx != -1;
        }
    }
}
