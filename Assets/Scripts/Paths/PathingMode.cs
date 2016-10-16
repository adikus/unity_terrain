using System.Collections.Generic;

namespace Assets.Scripts.Paths
{
    public class PathingMode
    {
        public int ID { get; set; }

        public bool UseStartDirection { get; set; }
        public bool UseGoalDirection { get; set; }
        public bool MatchZ { get; set; }
        public bool UpdateClosedSet { get; set; }

        public float HeightDiffCost { get; set; }
        public float BaseCost { get; set; }
        public float FinalCost { get; set; }

        public int OptimalLength { get; set; }

        public static Dictionary<int, PathingMode> Modes = new Dictionary<int, PathingMode>
        {
            {0, new PathingMode {ID = 0, UseStartDirection = false, UseGoalDirection = false, MatchZ = false, UpdateClosedSet = false, HeightDiffCost = 5, OptimalLength = 60}},
            {1, new PathingMode {ID = 1, MatchZ = false, HeightDiffCost = 5, OptimalLength = 25}},
            {2, new PathingMode {ID = 2, HeightDiffCost = 2.5f, FinalCost = 0.5f, OptimalLength = 10}},
            {3, new PathingMode {ID = 3, BaseCost = 3, FinalCost = 0.5f}}
        };

        public PathingMode()
        {
            UseStartDirection = true;
            UseGoalDirection = true;
            MatchZ = true;
            UpdateClosedSet = true;

            HeightDiffCost = 1;
            BaseCost = 1;
            FinalCost = 1;
        }
    }
}