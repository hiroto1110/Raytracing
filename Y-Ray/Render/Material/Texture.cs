using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using VecMath;

namespace YRay.Render.Material
{
    public interface ITexture
    {
        Vector3 GetColor(Vector2 uv);
    }

    public class TextureFill : ITexture
    {
        Vector3 Color { get; set; }

        public TextureFill(Vector3 color)
        {
            Color = color;
        }

        public Vector3 GetColor(Vector2 uv) => Color;
    }

    public class Texture : ITexture
    {
        public Vector3[] Pixels { get; }

        public Vector2i Size { get; }

        public Vector3 this[int x, int y]
        {
            get => Pixels[y * Size.x + x];
            set => Pixels[y * Size.x + x] = value;
        }

        public Texture(Vector2i size)
        {
            Size = size;
            Pixels = new Vector3[Size.x * Size.y];
        }

        public Vector3 GetColor(Vector2 uv)
        {
            return this[(int)Math.Round(uv.x * (Size.x - 1)), (int)Math.Round(uv.y * (Size.y - 1))];
        }

        public static Texture CreateFromFile(string path)
        {
            Bitmap bitmap = new Bitmap(path);

            Texture result = new Texture(new Vector2i(bitmap.Width, bitmap.Height));
            result.LoadBitmap(bitmap);

            return result;
        }

        public void LoadBitmap(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            byte[] buf = new byte[data.Stride * bitmap.Height];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    this[x, y] = Convert(x, y, data.Stride);
                }
            }

            Vector3 Convert(int x, int y, int stride)
            {
                int index = x * 3 + stride * y;
                byte b = buf[index + 0];
                byte g = buf[index + 1];
                byte r = buf[index + 2];

                return new Vector3(r / 255.0F, g / 255.0F, b / 255.0F);
            }
        }

        public Bitmap CreateBitmap()
        {
            var result = new Bitmap(Size.x, Size.y);

            byte[] buf = new byte[Size.x * Size.y * 3];

            for (int i = 0; i < Pixels.Length; i++)
            {
                Vector3 pix = Pixels[i];

                buf[i * 3 + 0] = Convert(pix.z);
                buf[i * 3 + 1] = Convert(pix.y);
                buf[i * 3 + 2] = Convert(pix.x);
            }

            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            result.UnlockBits(data);

            return result;

            byte Convert(float f)
            {
                if (f <= 0) return 0;
                if (f >= 1) return 255;
                return (byte)(255 * f);
            }
        }
    }
}
