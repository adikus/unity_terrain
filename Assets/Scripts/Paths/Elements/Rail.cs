using System.Collections.Generic;

namespace Assets.Scripts.Paths.Elements
{
    public class Rail : Base
    {
        public Rail(PathingJob pathingJob, IElement previous, Types type, int x, int y, float z, int direction)
            : base(pathingJob, previous, type, x, y, z, direction)
        {
        }
    }
}