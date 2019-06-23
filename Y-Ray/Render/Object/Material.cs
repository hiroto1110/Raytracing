using VecMath;

namespace YRay.Render.Object
{
    public class Material
    {
        public Vector3 Emission { get; set; }

        public Material Copy() => new Material() { Emission = Emission };
    }
}
