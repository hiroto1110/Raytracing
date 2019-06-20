using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using VecMath;

namespace YRay.Render.Object
{
   public  class Camera
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public float Fov { get; set; }

    }
}
