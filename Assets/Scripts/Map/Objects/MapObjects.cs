using System.Collections.Generic;
using Assets.Scripts.Map.Utils;

namespace Assets.Scripts.Map.Objects
{
    public class MapObjects
    {
        public static decimal Version = 0.2m;

        public List<Point2<int>> Cities { get; set; }
        public List<Point2<int>> Towns { get; set; }
    }
}
