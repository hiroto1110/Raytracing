using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using VecMath;
using YRay.Render;
using YRay.Render.Object;

namespace YRay
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        Renderer Renderer = new Renderer();

        public MainWindow()
        {
            InitializeComponent();

            Renderer.Scene.Camera.Position = new Vector3(0, 1F, -1.4F);
            Renderer.Scene.Camera.Rotation = Matrix3.LookAt(Vector3.UnitZ, Vector3.UnitY);

            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\test.obj")) { Material = new Material() { } });
            Renderer.Scene.Objects.Add(new Mesh(Mesh.CreateMeshFromWavefront("C:\\Projects\\Y-Ray\\Y-Ray\\bin\\Debug\\light.obj")) { Material = new Material() { Emission = new Vector3(1, 1, 1) * 10 } });
        }

        private async void ButtonClicked(object sender, RoutedEventArgs e)
        {
            image.Source = await Task.Factory.StartNew(() =>
            {
                var bitmap = Renderer.Render();

                using (Stream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);

                    return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            });

            e.Handled = true;
        }
    }
}
