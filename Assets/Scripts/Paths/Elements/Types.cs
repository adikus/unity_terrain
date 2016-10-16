using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Paths.Elements
{
    public class Types
    {
        public int ID { get; set; }
        public Vector3 PositionDiff { get; set; }
        public int DirectionDiff { get; set; }
        public float MaxZDiff { get; set; }
        public float BaseCost { get; set; }
        public bool Mirrorable { get; set; }
        public float ZDiffDelta = 0.2f;

        // Temp rendering
        // TODO: Replace by proper rendering in the future
        public List<Vector3> RenderPositions { get; set; }
        public List<int> RenderDirections { get; set; }

        public static Types RailStraight = new Types
        {
            ID = 0,
            PositionDiff = new Vector3(1, 0, 0),
            DirectionDiff = 0,
            MaxZDiff = 0.2f,
            BaseCost = 1,
            Mirrorable = false,
            RenderPositions = new List<Vector3> {new Vector3(0, 0, 0)},
            RenderDirections = new List<int> {0}
        };

        public static Types RailCurve902 = new Types
        {
            ID = 2,
            PositionDiff = new Vector3(1, 0, -2),
            DirectionDiff = 90,
            MaxZDiff = 0.2f,
            BaseCost = 4,
            Mirrorable = true,
            RenderPositions = new List<Vector3> {new Vector3(0, 0, -0.2f), new Vector3(0.8f, 0, -1)},
            RenderDirections = new List<int> {25, 65}
        };

        public static Types RailCurve903 = new Types
        {
            ID = 6,
            PositionDiff = new Vector3(2, 0, -3),
            DirectionDiff = 90,
            MaxZDiff = 0.2f,
            BaseCost = 4.5f,
            Mirrorable = true,
            RenderPositions = new List<Vector3> {new Vector3(0, 0, -0.1f), new Vector3(1, 0, -0.4f), new Vector3(1.6f, 0, -1), new Vector3(1.9f, 0, -2)},
            RenderDirections = new List<int> {10,35,55,80}
        };

        public static Types RailStraight21 = new Types
        {
            ID = 4,
            PositionDiff = new Vector3(2, 0, 1),
            DirectionDiff = 0,
            MaxZDiff = 0.2f,
            BaseCost = 2.5f,
            Mirrorable = true,
            RenderPositions = new List<Vector3> {new Vector3(0, 0, 0.25f), new Vector3(1, 0, 0.75f)},
            RenderDirections = new List<int> {-25, -25}
        };

        public static List<Types> RailTypes = new List<Types> {RailStraight, RailCurve902, RailCurve903, RailStraight21};

        public Types GetSpecific(float zDiff, bool mirrorred)
        {
            if (!Mirrorable && mirrorred) return null;

            var specific = new Types
            {
                ID = ID,
                PositionDiff = PositionDiff + new Vector3(0, zDiff, 0),
                DirectionDiff = DirectionDiff,
                MaxZDiff = MaxZDiff,
                BaseCost = BaseCost,
                Mirrorable = Mirrorable,
                RenderPositions = RenderPositions,
                RenderDirections = RenderDirections
            };

            if (!mirrorred) return specific;
            specific.ID++;
            specific.PositionDiff = Vector3.Scale(PositionDiff, new Vector3(1, 1, -1));
            specific.DirectionDiff *= -1;
            specific.RenderPositions = RenderPositions.Select(pos => Vector3.Scale(pos, new Vector3(1, 1, -1))).ToList();
            specific.RenderDirections = RenderDirections.Select(angle => angle * -1).ToList();
            return specific;
        }
    }
}
