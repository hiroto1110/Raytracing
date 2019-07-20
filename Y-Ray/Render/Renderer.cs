using System;
using System.Threading.Tasks;
using VecMath;
using YRay.Render.Material;

namespace YRay.Render
{
    class Renderer
    {
        public static readonly ParallelOptions PARALLEL_OPTION = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 16,
        };

        public Scene Scene { get; } = new Scene();

        public Vector2i RenderSize { get; set; } = new Vector2i(640, 360);

        private Random Rand { get; } = new Random();

        public Renderer()
        {
            Scene.Camera.Fov = (float)Math.PI / 4;
            Scene.Camera.Aspect = (float)RenderSize.y / RenderSize.x;
        }

        public RenderResult CreateRenderResult() => new RenderResult(RenderSize);

        private Vector3 RenderPixel(int x, int y, bool random)
        {
            float sampleX = x + (random ? ((float)Rand.NextDouble() - 0.5F) : 0);
            float sampleY = y + (random ? ((float)Rand.NextDouble() - 0.5F) : 0);
            float u = (sampleX / RenderSize.x) * 2 - 1;
            float v = (sampleY / RenderSize.y) * 2 - 1;

            return Scene.Raytrace(u, -v);
        }

        public void Render(RenderResult result, int sample, int totalSample, Func<Vector2i, bool> predicte, Action<float> action)
        {
            Scene.InitPreRendering();

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var blockSize = new Vector2i(1, 1);
            var blockNum = new Vector2i((int)Math.Ceiling((float)RenderSize.x / blockSize.x), (int)Math.Ceiling((float)RenderSize.y / blockSize.y));

            int count = 0;

            for (int blockX = 0; blockX < blockNum.x; blockX++)
            {
                for (int blockY = 0; blockY < blockNum.y; blockY++)
                {
                    var blockPos = new Vector2i(blockX, blockY);
                    var currentBlockSize = new Vector2i(dim => blockPos[dim] == (blockNum[dim] - 1) ? (RenderSize[dim] % blockSize[dim] != 0 ? RenderSize[dim] % blockSize[dim] : blockSize[dim]) : blockSize[dim]);

                    Parallel.For(0, currentBlockSize.x * currentBlockSize.y, PARALLEL_OPTION, i =>
                    {
                        int x = i % currentBlockSize.x;
                        int y = i / currentBlockSize.x;
                        int wolrd_x = blockPos.x * blockSize.x + x;
                        int wolrd_y = blockPos.y * blockSize.y + y;

                        if (predicte(new Vector2i(wolrd_x, wolrd_y)))
                        {
                            Vector3 pixel = Vector3.Zero;

                            for (int s = 0; s < sample; s++)
                            {
                                pixel += RenderPixel(wolrd_x, wolrd_y, totalSample > 0);
                            }

                            if (totalSample > 0)
                            {
                                result[wolrd_x, wolrd_y] = (result[wolrd_x, wolrd_y] * totalSample + pixel) / (totalSample + sample);
                            }
                            else
                            {
                                result[wolrd_x, wolrd_y] = pixel / sample;
                            }
                        }
                    });

                    count += currentBlockSize.x * currentBlockSize.y;

                    action(count / (float)(RenderSize.x * RenderSize.y));
                }
            }

            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds}ms");
        }
    }

    public class RenderResult : Texture
    {
        public bool IsUpdated { get; set; }

        public RenderResult(Vector2i size) : base(size)
        {
        }

        public bool CreateNoizePosArray(float e, out bool[,]  result)
        {
             result = new bool[Size.x, Size.y];

            bool any = false;

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    Vector3 noize = Vector3.Zero;

                    int count = 0;

                    if (x != 0)
                    {
                        noize += this[x - 1, y];
                        count++;
                    }
                    if (x != Size.x - 1)
                    {
                        noize += this[x + 1, y];
                        count++;
                    }

                    if (y != 0)
                    {
                        noize += this[x, y - 1];
                        count++;
                    }
                    if (y != Size.y - 1)
                    {
                        noize += this[x, y + 1];
                        count++;
                    }

                    noize = noize / count - this[x, y];

                    bool flag = noize.LengthSquare() > e * e;
                    result[x, y] = flag;

                    if(flag)
                    {
                        any = true;
                    }
                }
            }
            return any;
        }
    }
}
