using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assets.Scripts.Map;
using Assets.Scripts.Map.Utils;
using Assets.Scripts.Paths.Elements;
using Object = UnityEngine.Object;
using Types = Assets.Scripts.Paths.Elements.Types;

namespace Assets.Scripts.Paths
{
    public class PathAutoComplete
    {
        public GameObject Start;
        public GameObject Goal;
        public IElement StartPart;
        public Tile StartTile;
        public Point3<int, float> GoalPoint;
        public int GoalDirection;
        public Tile GoalTile;

        public int Angle = 0;

        public int DX;
        public int DY;
        public float DZ;
        public int DA = 0;

        public List<IElement> Parts;

        public PathAutoComplete()
        {
            var cityPrefab = (GameObject) Resources.Load("prefabs/Paths/ArrowBox", typeof(GameObject));
            Start = Object.Instantiate(cityPrefab);
            Goal = Object.Instantiate(cityPrefab);

            Start.GetComponent<Renderer>().material.color = Color.green;
            Goal.GetComponent<Renderer>().material.color = Color.red;
        }

        public void SetStartPosition(IElement part)
        {
            if (part == null)
            {
                StartPart = null;
                return;
            }
            var tile = GameControl.Map.GetTile(part.EndX, part.EndY);
            var cubeOffset = new Vector3(0.5f, 0.5f, 0.5f);
            StartPart = part;
            StartTile = tile;
            Angle = part.EndDirection - 90;
            Start.transform.position = GameControl.Terrain.Offset + cubeOffset + new Vector3(part.EndX, part.EndZ/2, part.EndY);
            Start.transform.rotation = Quaternion.identity;
            Start.transform.Rotate(Vector3.up, Angle);

            //if (GoalTile != null) Autocomplete();
        }

        public void SetGoalPosition(Point3<int, float> goal, int goalDirection)
        {
            var tile = GameControl.Map.GetTile(goal.X, goal.Y);
            var cubeOffset = new Vector3(0.5f, 0.5f, 0.5f);
            GoalTile = tile;
            GoalPoint = goal;
            GoalDirection = goalDirection;
            DA = GoalDirection - StartPart.EndDirection;
            while (DA > 180) DA -= 360;
            while (DA < -90) DA += 360;
            Goal.transform.position = GameControl.Terrain.Offset + cubeOffset + new Vector3(goal.X, goal.Z/2, goal.Y);
            Goal.transform.rotation = Quaternion.identity;
            Goal.transform.Rotate(Vector3.up, Angle + DA);

            //if (StartTile != null) Autocomplete();
        }

        public IElement Autocomplete()
        {
//            if (Parts != null && GameControl.Paths.CurrentJob.Mode.ID < 3)
//            {
//                foreach (var p in Parts)
//                {
//                    p.Hide();
//                }
//            }

            if (StartPart == null) return null;

            Parts = new List<IElement> { StartPart };
            //Parts.Add(new Rail(null, null, Types.RailStraight, StartTile.X, StartTile.Y, StartTile.AverageHeight(), 90 + Angle));

            GameControl.UI.DebugLines[4] = "From: " + StartPart.X + "," + StartPart.Y + "," + StartPart.Z +
                                           " To: " + GoalPoint.X + "," + GoalPoint.Y + "," + GoalPoint.Z;

            var diff = new Vector3(
                GoalPoint.X - StartPart.EndX,
                GoalPoint.Z - StartPart.EndZ,
                GoalPoint.Y - StartPart.EndY
            );
            var correction = new Vector3(0, 0, 1);

            diff = Quaternion.AngleAxis(-Angle - DA, Vector3.up) * diff;
//            correction = Quaternion.AngleAxis(-DA, Vector3.up) * correction;
//            diff += correction;

            DX = (int) Math.Round(diff.x);
            DY = (int) Math.Round(diff.z);
            DZ = diff.y;

            GameControl.UI.DebugLines[5] = "DX: " + DX + " DY: " + DY + " DZ: " + DZ + " DA: " + DA;

            FindAndExecuteStrategy();

            if (Parts.Count > 1 && Math.Abs(DZ) > 0.2f && GameControl.Paths.CurrentJob.Mode.MatchZ)
            {
                var expectedDz = 0.2f * Math.Sign(DZ);
                var current = Parts.Last();
                var sum = 0f;
                var head = current;
                while (current != null)
                {
                    if (Math.Abs(current.EndZ - current.Z - expectedDz) > Mathf.Epsilon)
                    {
                        sum += 0.2f;
                    }
                    current = current.Previous;
                    if (current != null) head = current;
                }
                Debug.Log("DZ: " + DZ + " Failed to match DZ, possible adjustment: " + sum);
                var accumulatedDz = 0f;
                if (sum > Math.Abs(DZ))
                {
                    current = head;
                    while (Math.Abs(DZ) > 0.2f && current != null)
                    {
                        current.EndZ += accumulatedDz;
                        current.Z += accumulatedDz;
                        if (Math.Abs(current.EndZ - current.Z - expectedDz) > Mathf.Epsilon)
                        {
                            DZ -= expectedDz;
                            accumulatedDz += expectedDz;
                            current.EndZ += expectedDz;
                        }

                        current = current.Next;
                    }
                }
            }


//            foreach (var p in Parts)
//            {
//                p.Show();
//            }

            return Parts.Count > 1 ? Parts.Last() : null;
        }

        private void AddPart(Types type, bool mirrored)
        {
            var dz = Math.Abs(DZ)/(Math.Abs(DX) + Math.Abs(DY)) > 0.1f ? Math.Sign(DZ) * 0.2f : 0;
            var part = Parts.Last().AddPart(type, dz, mirrored);
            Parts.Add(part);
            var diff = part.Type.PositionDiff;
            diff = Quaternion.AngleAxis(180 + part.Direction - Angle - DA, Vector3.up) * diff;
            DX += (int) Math.Round(diff.x);
            DY += (int) Math.Round(diff.z);
            DZ -= diff.y;

            if (Math.Abs(DX) > 2000 || Math.Abs(DY) > 2000)
            {
                throw new ArgumentOutOfRangeException("PathAutoComplete", "Infinte loop detected.");
            }

            //Debug.Log("DX: " + Math.Round(diff.x) + " DY: " + Math.Round(diff.z) + " DZ: " + diff.y + " Type: " + part.Type.ID + " Dir: " + part.Direction);
        }

        private void FindAndExecuteStrategy()
        {
            if (DA == 0)
            {
                if (DY * -1 >= Math.Abs(DX) * 2)
                {
                    Straight();
                }
                else if (Math.Abs(DX) >= 5 && DY <= -5)
                {
                    Curve0903();
                }
                else if (Math.Abs(DX) >= 3 && DY <= -3)
                {
                    Curve0902();
                }
            }
            else if (Math.Abs(DA) == 90)
            {
                if (DY <= -3 && Math.Sign(DA) * DX >= 3)
                {
                    Curve90903();
                }
                else if (DY <= -2 && Math.Sign(DA) * DX >= 1)
                {
                    Curve90902();
                }
                else if (DY >= 3 && Math.Sign(DA) * DX >= 7)
                {
                    Curve90903X3();
                }
                else if (DY >= -1 && Math.Sign(DA) * DX >= 4)
                {
                    Curve90902X3();
                }
            }
            else if (DA == 180)
            {
                if (Math.Abs(DX) >= 5)
                {
                    Curve180903();
                }
                else if (Math.Abs(DX) >= 3)
                {
                    Curve180902();
                }
            }
        }

        private void Curve0902()
        {
            AddPart(Types.RailCurve902, DX > 0);
            Curve90902();
        }

        private void Curve0903()
        {
            AddPart(Types.RailCurve903, DX > 0);
            Curve90903();
        }

        private void Curve90902()
        {
            while (Math.Abs(Math.Abs(DX) - 1) > 0)
            {
                AddPart(Types.RailStraight, false);
            }
            AddPart(Types.RailCurve902, DX < 0);
            Straight();
        }

        private void Curve90903()
        {
            while (Math.Abs(Math.Abs(DX) - 2) > 0)
            {
                AddPart(Types.RailStraight, false);
            }
            AddPart(Types.RailCurve903, DX < 0);
            Straight();
        }

        private void Curve90902X3()
        {
            AddPart(Types.RailCurve902, DX > 0);
            Curve180902();
        }

        private void Curve90903X3()
        {
            AddPart(Types.RailCurve903, DX > 0);
            Curve180903();
        }

        private void Curve180902()
        {
            while (DY > -1)
            {
                AddPart(Types.RailStraight, false);
            }
            AddPart(Types.RailCurve902, DX < 0);
            Curve90902();
        }

        private void Curve180903()
        {
            while (DY > -1)
            {
                AddPart(Types.RailStraight, false);
            }
            AddPart(Types.RailCurve903, DX < 0);
            Curve90903();
        }

        private void Straight()
        {
            while (Math.Abs(DX) > 0)
            {
                AddPart(Types.RailStraight21, DX < 0);
            }

            while (DY < 0)
            {
                AddPart(Types.RailStraight, false);
            }
        }
    }
}