using VecMath;

namespace YRay.Render.Object
{
    public abstract class Light
    {
        public Vector3 Pos { get; set; }
        public Vector3 Color { get; set; }
    }
}
