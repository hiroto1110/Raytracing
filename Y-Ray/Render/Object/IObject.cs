using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecMath;
using VecMath.Geometry;
using YRay.Render.Material;

namespace YRay.Render.Object
{
    public interface IObject
    {
        AABoundingBox AABB { get; }

        Matrix4 Transform { get; }
        Matrix4 TransformInv { get; }

        void InitPreRendering();

        bool DistanceToRay(Ray ray, HitResult result);
    }

    public abstract class ObjectBase : IObject
    {
        public AABoundingBox AABB { get; set; }

        public Matrix4 Transform { get; set; } = Matrix4.Identity;
        public Matrix4 TransformInv { get; set; } = Matrix4.Identity;

        public abstract bool DistanceToRay(Ray ray, HitResult result);

        public virtual void InitPreRendering()
        {
            TransformInv = Matrix4.InverseTransform(Transform);
        }
    }

    public class Sphere : ObjectBase
    {
        public IMaterial Material { get; set; }

        public float Range { get; set; }

        public Sphere(float range)
        {
            Range = range;
            AABB = new AABoundingBox(new Vector3(1, 1, 1) * -Range, new Vector3(1, 1, 1) * Range);
        }

        public override bool DistanceToRay(Ray ray, HitResult result)
        {
            var pos = Transform.Translation;

            float a = VMath.Dot(ray.vec, pos - ray.pos);
            float e = (pos - ray.pos).LengthSquare();

            float f = Range * Range - e + a * a;

            if (f < 0) return false;

            float distance = a - (float)Math.Sqrt(f);

            if (result.distance > distance && distance > 0.05)
            {
                result.normal = VMath.Normalize((ray.pos + ray.vec * distance) - pos);
                result.material = Material;
                result.distance = distance;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class HitResultPolygon
    {
        public Polygon poly;
        public Vector3 barycentricPos;
        public float distance;
    }

    public class Mesh : ObjectBase
    {
        public IMaterial Material { get; set; }

        private OctagonalNode OctagonalNode { get; }

        public static Polygon[] CreateMeshFromWavefront(string path)
        {
            var polygons = new List<Polygon>();

            using (var reader = new StreamReader(path))
            {
                var positions = new List<Vector3>();
                var normals = new List<Vector3>();
                var texcoords = new List<Vector2>();

                while (reader.Peek() > -1)
                {
                    string line = reader.ReadLine();

                    string[] tokens = line.Split();

                    switch (tokens[0])
                    {
                        case "v":
                            positions.Add(Parse3(tokens));
                            break;

                        case "vn":
                            normals.Add(Parse3(tokens));
                            break;

                        case "vt":
                            texcoords.Add(Parse2(tokens));
                            break;

                        case "f":
                            var poly_vertices = new List<Vertex>();

                            for (int i = 0; i < 3; i++)
                            {
                                string[] index = tokens[1 + i].Split('/');

                                Vertex v;

                                switch (index.Length)
                                {
                                    case 1:
                                        v = new Vertex(positions[int.Parse(index[0])], Vector3.UnitY, Vector2.Zero);
                                        break;

                                    case 2:
                                        v = new Vertex(positions[int.Parse(index[0])], Vector3.UnitY, texcoords[int.Parse(index[1])]);
                                        break;

                                    case 3:
                                        var uv = index[1] == "" ? Vector2.Zero : texcoords[int.Parse(index[1]) - 1];
                                        v = new Vertex(positions[int.Parse(index[0]) - 1], normals[int.Parse(index[2]) - 1], uv);
                                        break;

                                    default:
                                        throw new FileFormatException();
                                }
                                poly_vertices.Add(v);
                            }
                            polygons.Add(new Polygon(poly_vertices.ToArray()));
                            break;
                    }
                }
                return polygons.ToArray();
            }

            Vector3 Parse3(string[] s) => new Vector3(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
            Vector2 Parse2(string[] s) => new Vector2(float.Parse(s[1]), float.Parse(s[2]));
        }

        public Mesh(Polygon[] polygons)
        {
            Vector3 min = polygons[0].V1.pos;
            Vector3 max = polygons[0].V1.pos;

            foreach (var poly in polygons)
            {
                foreach (var v in poly.vertices)
                {
                    min = VMath.Min(min, v.pos);
                    max = VMath.Max(max, v.pos);
                }
            }
            AABB = new AABoundingBox(min, max);

            OctagonalNode = new OctagonalNode(AABB);
            OctagonalNode.Polygons.AddRange(polygons);
            OctagonalNode.InitPolygons(0, 9, 10000);
        }

        public override bool DistanceToRay(Ray ray, HitResult result)
        {
            var resultPoly = new HitResultPolygon
            {
                distance = result.distance
            };

            if (OctagonalNode.DistanceToRay(Ray.Transform(ray, TransformInv), resultPoly))
            {
                result.distance = resultPoly.distance;
                result.normal = resultPoly.poly.normal * TransformInv;
                result.uv = resultPoly.poly.CalcUV(resultPoly.barycentricPos);
                result.material = Material;

                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class OctagonalNode
    {
        public AABoundingBox AABB { get; private set; }

        public List<Polygon> Polygons { get; private set; } = new List<Polygon>();

        public OctagonalNode[] Childs { get; private set; } = { };

        public OctagonalNode(AABoundingBox aabb)
        {
            AABB = aabb;
        }

        public void InitPolygons(int depth, int maxDepth, int maxNumPolygons)
        {
            if (depth > maxDepth || Polygons.Count < maxNumPolygons)
            {
                UpdateAABB();
                return;
            }

            var childs = DivideAABB(AABB).Select(a => new OctagonalNode(a)).ToList();

            var newList = new List<Polygon>(Polygons);
            Polygons = new List<Polygon>();

            foreach (var poly in newList)
            {
                (childs.FirstOrDefault(c => IsHoldPolygon(c.AABB, poly)) ?? this).Polygons.Add(poly);
            }

            Childs = childs.Where(c => c.Polygons.Count > 0).ToArray();

            foreach (var child in Childs)
            {
                child.InitPolygons(depth + 1, maxDepth, maxNumPolygons);
            }

            UpdateAABB();
        }

        private void UpdateAABB()
        {
            Vector3 min = new Vector3(1, 1, 1) * 1E+6F;
            Vector3 max = new Vector3(1, 1, 1) * -1E+6F;

            foreach (var poly in Polygons)
            {
                foreach (var v in poly.vertices)
                {
                    min = VMath.Min(min, v.pos);
                    max = VMath.Max(max, v.pos);
                }
            }

            foreach (var child in Childs)
            {
                min = VMath.Min(min, child.AABB.PosMin);
                max = VMath.Max(max, child.AABB.PosMax);
            }

            AABB = new AABoundingBox(min, max);
        }

        public bool DistanceToRay(Ray ray, HitResultPolygon result)
        {
            bool hit = false;

            if (DistanceToRayLocal(ray, result))
            {
                hit = true;
            }

            if (Childs.Length == 0) return hit;

            var candicates = new List<(OctagonalNode obj, float start)>();

            foreach (var node in Childs)
            {
                if (node.AABB.CalculateTimeToIntersectWithRay(ray, out float start, out float end) && start < result.distance)
                {
                    if (end <= 0) continue;

                    if (result.distance > start)
                    {
                        candicates.Add((node, start));
                    }
                }
            }

            foreach ((var node, float f) in candicates.OrderBy(o => o.start))
            {
                if (node.DistanceToRay(ray, result))
                {
                    hit = true;
                    break;
                }
            }
            return hit;
        }

        private bool DistanceToRayLocal(Ray ray, HitResultPolygon result)
        {
            bool hit = false;

            for (int i = 0; i < Polygons.Count; i++)
            {
                var poly_temp = Polygons[i];
                float distance = poly_temp.CalculateTimeToIntersectWithRay(ray);

                if (result.distance > distance && distance > 0.05 && poly_temp.BarycentricPos(ray, out Vector3 baryPos))
                {
                    hit = true;

                    result.poly = poly_temp;
                    result.distance = distance;
                    result.barycentricPos = baryPos;
                }
            }

            return hit;
        }

        private static bool IsHoldPolygon(AABoundingBox aabb, Polygon poly)
        {
            return aabb.IsHoldPos(poly.V1.pos) && aabb.IsHoldPos(poly.V2.pos) && aabb.IsHoldPos(poly.V3.pos);
        }

        private static IEnumerable<AABoundingBox> DivideAABB(AABoundingBox parent)
        {
            var center = (parent.PosMin + parent.PosMax) * 0.5F;

            for (int i = 0; i < 8; i++)
            {
                int[] dimFlag = { i / 4, i % 4 / 2, i % 4 % 2 };

                Vector3 min = Vector3.Zero;
                Vector3 max = Vector3.Zero;

                for (int dim = 0; dim < 3; dim++)
                {
                    min[dim] = dimFlag[dim] == 1 ? center[dim] : parent.PosMin[dim];
                    max[dim] = dimFlag[dim] == 1 ? parent.PosMax[dim] : center[dim];
                }

                yield return new AABoundingBox(min, max);
            }
        }
    }
}
