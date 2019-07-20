using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VecMath;
using YRay.Render;
using YRay.Render.Material;
using YRay.Render.Object;

namespace YRay
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        Renderer Renderer = new Renderer();
        DispatcherTimer Timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            Renderer.Scene.Camera.Position = new Vector3(0, 1.5F, -2F);
            Renderer.Scene.Camera.Rotation = Matrix3.LookAt(Vector3.UnitZ, Vector3.UnitY);

            //Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("D:\\Work\\Project\\Summer\\Object\\Plant\\YoshinoCherry\\pop\\cherry_10_1.obj"))
            //{
            //    Material = new MaterialLambert()
            //    {
            //        Albedo = new TextureFill(new Vector3(0.8F, 0.8F, 0.8F)),
            //        Roughness = 1,
            //    }
            //});

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\test1.obj"))
            {
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.8F, 0.8F, 0.8F)),
                    Roughness = 1,
                }
            });

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\test2.obj"))
            {
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.2F, 0.8F, 0.2F)),
                    Roughness = 1,
                }
            });

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\test3.obj"))
            {
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(1F, 0.2F, 0.2F)),
                    Roughness = 1,
                }
            });

            Renderer.Scene.Objects.Add(new Sphere(0.6F)
            {
                Transform = new Matrix4(Matrix3.Identity, new Vector3(0F, 2F, 1.4F)),
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.8F, 0.8F, 0.3F)),
                    Roughness = 0.01F,
                },
            });

            Renderer.Scene.Objects.Add(new Sphere(0.3F)
            {
                Transform = new Matrix4(Matrix3.Identity, new Vector3(1F, 1.6F, 0F)),
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.8F, 0.8F, 0.8F)),
                    Roughness = 0.8F,
                },
            });

            Renderer.Scene.Objects.Add(new Sphere(0.4F)
            {
                Transform = new Matrix4(Matrix3.Identity, new Vector3(-1F, 0.4F, 0F)),
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.3F, 0.8F, 0.8F)),
                    Roughness = 0.4F,
                },
            });

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\test4.obj"))
            {
                Material = new MaterialLambert()
                {
                    Albedo = new TextureFill(new Vector3(0.2F, 0.2F, 0.8F)),
                    Roughness = 1,
                }
            });

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\light.obj"))
            {
                Material = new MaterialLambert()
                {
                    Emission = new Vector3(1, 1, 1) * 20, //
                    Albedo = new TextureFill(new Vector3(1F, 1F, 1F)),
                    Roughness = 1,
                }
            });

            Timer.Interval = new TimeSpan(1000);
            Timer.Tick += OnTick;
            Timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (RenderResult == null) return;

            bool isUpdated;

            lock (RenderResult)
            {
                isUpdated = RenderResult.IsUpdated;
            }

            if (isUpdated)
            {
                Bitmap bitmap = RenderResult.CreateBitmap();

                using (Stream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);

                    image.Source = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }

                lock (RenderResult)
                {
                    RenderResult.IsUpdated = false;
                }
            }
        }

        private RenderResult RenderResult { get; set; }

        private async void ButtonClicked(object sender, RoutedEventArgs e)
        {
            await Task.Factory.StartNew(Render);
            await Task.Factory.StartNew(() => RenderPass(10, 0.02F));

            e.Handled = true;
        }

        private void Render()
        {
            RenderResult = Renderer.CreateRenderResult();

            Renderer.Render(RenderResult, 1, 0, p => true, progress =>
            {
                lock (RenderResult) RenderResult.IsUpdated = true;
                Console.WriteLine(progress);
            });
        }

        private void RenderPass(int pass, float epsilon)
        {
            int count = 0;

            while (count++ < pass && RenderResult.CreateNoizePosArray(epsilon, out bool[,] postions))
            {
                Console.WriteLine("pass " + count);

                Renderer.Render(RenderResult, 3, count * 3 - 2, p => postions[p.x , p.y], progress =>
                 {
                     lock (RenderResult) RenderResult.IsUpdated = true;
                     Console.WriteLine(progress);
                 });
            }
        }
    }
}
