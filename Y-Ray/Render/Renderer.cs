using System;
using System.Drawing;
using System.Drawing.Imaging;
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

        public Size RenderSize { get; set; } = new Size(320, 180);

        public Renderer()
        {
            Scene.Camera.Fov = (float)Math.PI / 3;
            Scene.Camera.Aspect = (float)RenderSize.Height / RenderSize.Width;
        }

        public Color RenderPixel(int x, int y)
        {
            return Convert(Scene.Raytrace((x / (float)RenderSize.Width) * 2 - 1, -((y / (float)RenderSize.Height) * 2 - 1)));

            Color Convert(Vector3 vec)
            {
                var col = VMath.Clamp(vec, Vector3.Zero, new Vector3(1, 1, 1));
                return Color.FromArgb((int)(255 * col.x), (int)(255 * col.y), (int)(255 * col.z));
            }
        }

        public Bitmap Render()
        {
            var result = new Bitmap(RenderSize.Width, RenderSize.Height);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            byte[] buf = new byte[RenderSize.Width * RenderSize.Height * 3];

            //Parallel.For(0, RenderSize.Width * RenderSize.Height, PARALLEL_OPTION, i =>
            for (int i = 0; i < RenderSize.Width * RenderSize.Height; i++)
            {
                int x = i % RenderSize.Width;
                int y = i / RenderSize.Width;

                // Console.WriteLine(x + ", " + y);

                var color = RenderPixel(x, y);

                //  lock (buf)
                {
                    buf[i * 3 + 0] = color.R;
                    buf[i * 3 + 1] = color.G;
                    buf[i * 3 + 2] = color.B;
                }
                if (i % 100 == 0) Console.WriteLine(i / (float)(RenderSize.Width * RenderSize.Height));
            }
            // );

            sw.Stop();
            Console.WriteLine($"　{sw.ElapsedMilliseconds}ミリ秒");

            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            result.UnlockBits(data);

            return result;
        }
    }
}
