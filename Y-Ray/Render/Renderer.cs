using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VecMath;

namespace YRay.Render
{
    class Renderer
    {
        public static readonly ParallelOptions PARALLEL_OPTION = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 16,
        };

        public Scene Scene { get; } = new Scene();

        public Size RenderSize { get; set; } = new Size(1280, 720);

        public Renderer()
        {
            Scene.Camera.Fov = (float)Math.PI / 3;
            Scene.Camera.Aspect = (float)RenderSize.Height / RenderSize.Width;
        }

        public Vector3[,] RenderPixelBlock(Point start, Size blockSize)
        {
            float[] ua = Enumerable.Range(0, blockSize.Width).Select(x => ((start.X + x) / (float)RenderSize.Width) * 2 - 1).ToArray();
            float[] va = Enumerable.Range(0, blockSize.Height).Select(y => ((start.Y + y) / (float)RenderSize.Height) * -2 + 1).ToArray();

            return Scene.RayTrace(ua, va);
        }

        public Bitmap Render()
        {
            var result = new Bitmap(RenderSize.Width, RenderSize.Height);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            byte[] buf = new byte[RenderSize.Width * RenderSize.Height * 3];

            var blockSize = new Size(16, 9);
            var blockNum = new Size((int)Math.Ceiling((float)RenderSize.Width / blockSize.Width), (int)Math.Ceiling((float)RenderSize.Height / blockSize.Height));

            object lockobj = new object();
            int count = 0;

            Parallel.For(0, blockNum.Width * blockNum.Height, i =>
            {
                int block_x = i % blockNum.Width;
                int block_y = i / blockNum.Width;

                //Console.WriteLine(block_x + ", " + block_y);

                int blockWidth = block_x == (blockNum.Width - 1) ? RenderSize.Width % blockSize.Width : blockSize.Width;
                int blockHeight = block_y == (blockNum.Height - 1) ? RenderSize.Height % blockSize.Height : blockSize.Height;

                if (blockWidth == 0) blockWidth = blockSize.Width;
                if (blockHeight == 0) blockHeight = blockSize.Height;

                Vector3[,] colors = RenderPixelBlock(new Point(block_x * blockSize.Width, block_y * blockSize.Height), new Size(blockWidth, blockHeight));

                lock(lockobj)
                {
                    count += blockWidth * blockHeight;
                    Console.WriteLine(count / (float)(blockNum.Width * blockNum.Height));
                }

                for (int x = 0; x < blockWidth; x++)
                {
                    for (int y = 0; y < blockHeight; y++)
                    {
                        int wolrd_x = block_x * blockSize.Width + x;
                        int wolrd_y = block_y * blockSize.Height + y;
                        int index = wolrd_x + wolrd_y * RenderSize.Width;

                        var color = Convert(colors[x, y]);

                        //  lock (buf)
                        {
                            buf[index * 3 + 0] = color.R;
                            buf[index * 3 + 1] = color.G;
                            buf[index * 3 + 2] = color.B;
                        }
                    }
                }
            });

            sw.Stop();
            Console.WriteLine($"　{sw.ElapsedMilliseconds}ミリ秒");

            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            result.UnlockBits(data);

            return result;

            Color Convert(Vector3 vec)
            {
                var col = VMath.Clamp(vec, Vector3.Zero, new Vector3(1, 1, 1));
                return Color.FromArgb((int)(255 * col.x), (int)(255 * col.y), (int)(255 * col.z));
            }
        }
    }
}
