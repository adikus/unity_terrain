using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Paths.Elements
{
    public class Base : IElement
    {
        public int Direction { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public float Z { get; set; }
        public Types Type { get; set; }

        public int EndX { get; set; }
        public int EndY { get; set; }
        public float EndZ { get; set; }
        public int EndDirection { get; set; }

        public float Cost { get; set; }

        private List<GameObject> _objs;

        protected readonly PathingJob PathingJob;

        public IElement Previous { get; set; }
        public IElement Next { get; set; }

        public Base(PathingJob pathingJob, IElement previous, Types type, int x, int y, float z, int direction)
        {
            PathingJob = pathingJob;

            Previous = previous;
            Type = type;
            X = x;
            Y = y;
            Z = z;
            Direction = direction;

            var path = GetTypeVector();
            path = Quaternion.AngleAxis(Direction, Vector3.up) * path;
            var end = path + new Vector3(X, Z, Y);
            EndX = (int) Math.Round(end.x);
            EndY = (int) Math.Round(end.z);
            EndZ = end.y;
            var angle = GetTypeAngle();
            EndDirection = Direction + angle;
            if (EndDirection >= 360) EndDirection -= 360;

            if (PathingJob == null) return;

            if (!PathingJob.Mode.MatchZ)
            {
                var map = GameControl.Map;
                var tile = map.GetTile(X, Y);
                EndZ = tile.AverageHeight();
            }

            Cost = (Previous != null ? Previous.Cost : 0) + PartCost();
            if (Previous != null) Previous.Next = this;
        }

        public List<IElement> PossibleNext()
        {
            var parts = new List<IElement>();

            foreach (var type in Types.RailTypes)
            {
                var zFrom = PathingJob.Mode.MatchZ ? -type.MaxZDiff : 0;
                var zTo = PathingJob.Mode.MatchZ ? type.MaxZDiff : 0;

                for (var i = zFrom; i <= zTo; i += type.ZDiffDelta)
                {
                    parts.Add(new Rail(PathingJob, this, type.GetSpecific(i, false), EndX, EndY, EndZ, EndDirection));
                    if (type.Mirrorable)
                    {
                        parts.Add(new Rail(PathingJob, this, type.GetSpecific(i, true), EndX, EndY, EndZ, EndDirection));
                    }
                }
            }

            return parts;
        }

        public IElement AddPart(Types type, float z, bool mirrored)
        {
            return new Rail(PathingJob, this, type.GetSpecific(z, mirrored), EndX, EndY, EndZ, EndDirection);
        }

        private Vector3 GetTypeVector()
        {
            return Type.PositionDiff;
        }

        private int GetTypeAngle()
        {
            return Type.DirectionDiff;
        }

        private float GetTypeBaseCost()
        {
            return Type.BaseCost;
        }

        public void Show()
        {
            if (_objs != null) return;
            _objs = new List<GameObject>();

            var i = 0;

            var cubeOffset = new Vector3(0.5f, 0.125f, 0.5f);

            foreach (var position in Type.RenderPositions)
            {
                var angle = Type.RenderDirections[i];
                var obj = GameControl.Paths.InstantiateType(0);
                var rotatedPosition = Quaternion.AngleAxis(Direction, Vector3.up) * position;
                obj.transform.position = GameControl.Terrain.Offset + cubeOffset + rotatedPosition + new Vector3(X, EndZ/2, Y);
                obj.transform.Rotate(Vector3.up, angle + Direction);
                obj.GetComponent<Renderer>().material.color = Color.red;
                _objs.Add(obj);
                i++;
            }
        }

        public void Hide()
        {
            if (_objs == null) return;
            foreach (var obj in _objs)
            {
                Object.Destroy(obj);
            }
            _objs = null;
        }

        public bool IsPossible()
        {
            var map = GameControl.Map;
            var endTile = map.GetTile(EndX, EndY);
            if (endTile.Dummy()) return false;
            if (Z < -10) return false;
            if (endTile.AverageHeight() < 0 && Type != Types.RailStraight) return false;
            if (PathingJob.Mode.MatchZ)
            {
                if (endTile.AverageHeight() < 0 && Z < 1) return false;
                var current = Previous;
                while (current != null)
                {
                    if (current.X == X && current.Y == Y)
                    {
                        if (Math.Abs(current.Z - Z) < 1f) return false;
                        if (current.Direction == Direction) return false;
                    }
                    current = current.Previous;
                }
            }

            return true;
        }

        public float PartCost()
        {
            var map = GameControl.Map;
            var tile = map.GetTile(X, Y);
            var endTile = map.GetTile(EndX, EndY);
            var baseCost = GetTypeBaseCost();
            var heightDiffCost = Math.Abs(Z - EndZ);
            var heightCost = Math.Abs(Z - tile.AverageHeight());
            heightCost += Math.Abs(EndZ - endTile.AverageHeight());
            var waterCost = Z < 0 ? -Z : 0;
            heightDiffCost *= PathingJob.Mode.HeightDiffCost;
            baseCost *= PathingJob.Mode.BaseCost;
            var finalCost = baseCost + 5*heightDiffCost + 2*heightCost + waterCost;
            finalCost *= PathingJob.Mode.FinalCost;
            return finalCost;
        }

        public new string ToString()
        {
            return "(" + X + "," + Y + "," + Z + ") -> (" + EndX + "," + EndY + "," + EndZ + "); " + Direction + " -> " + EndDirection + " [" + Cost + "]";
        }
    }
}