using System.Collections.Generic;
using System.IO;
using VecMath;
using VecMath.Geometry;

namespace YRay.Render.Object
{
    public interface IObject
    {
        AABoundingBox AABB { get; }

        bool DistanceToRay(Ray ray, float minDistance, out Polygon tri, out float distance, out Material material);
    }

    public class Mesh : IObject
    {
        public AABoundingBox AABB { get; }
        public Polygon[] Polygons { get; }
        public Matrix4 Matrix { get; set; } = Matrix4.Identity;
        public Material Material { get; set; }

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
            Polygons = polygons;

            Vector3 min = Polygons[0].V1.pos;
            Vector3 max = Polygons[0].V1.pos;

            foreach (var poly in Polygons)
            {
                foreach (var v in poly.vertices)
                {
                    min = VMath.Min(min, v.pos);
                    max = VMath.Max(max, v.pos);
                }
            }

            AABB = new AABoundingBox(min, max);
        }

        public bool DistanceToRay(Ray ray, float minDistance, out Polygon poly, out float distance, out Material material)
        {
            float min = minDistance;
            int idx = -1;

            poly = new Polygon();
            distance = minDistance;
            material = Material;

            for (int i = 0; i < Polygons.Length; i++)
            {
                var poly_temp = Polygons[i];
                float time = poly_temp.CalculateTimeToIntersectWithRay(ray);

                if (min > time && time > 0 && poly_temp.IsIntersectWithRay(ray))
                {
                    min = time;
                    idx = i;

                    poly = poly_temp;
                    distance = time;
                    material = Material;
                }
            }
            return idx != -1;
        }
    }
}
