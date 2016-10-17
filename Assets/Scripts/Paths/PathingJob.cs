using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts.Map.Utils;
using Assets.Scripts.Paths.Elements;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.Paths
{
    public class PathingJob
    {
        public static int LastID = 0;

        public Point3<int, float> Start { get; set; }
        public Point3<int, float> Goal { get; set; }

        public int ID;

        public int StartDirection { get; set; }
        public int GoalDirection { get; set; }

        public PathingMode Mode { get; set; }
        public float MaxEstimate { get; set; }
        public bool Done;
        public bool Success;

        public SortedList<float, IElement> Heap;
        public Dictionary<string, IElement> ClosedSet;
        public Stopwatch Timer;
        public int Counter;

        private IElement _bestPath;
        private float _bestCost;

        private class CostComparer : IComparer<float>
        {
            public int Compare(float i1, float i2)
            {
                return i1 < i2 ? -1 : 1;
            }
        }

        public PathingJob()
        {
            Heap = new SortedList<float, IElement>(new CostComparer());
            ClosedSet = new Dictionary<string, IElement>();
            Timer = new Stopwatch();

            ID = ++LastID;
        }

        public void StartJob()
        {
            if (Mode.UseStartDirection)
            {
                AddStartPart(StartDirection);
            }
            else
            {
                for (var a = 0; a <= 360; a += 90)
                {
                    AddStartPart(a);
                }
            }

            GameControl.UI.DebugLines[1] = "Current job: ID: " + ID + " From " + Start.X + "," + Start.Y + "," + Start.Z + " To " +
                                           Goal.X + "," + Goal.Y + "," + Goal.Z + " ; Mode " + Mode.ID;
            Debug.Log("Starting Pathing Job ID: " + ID + " From " + Start.X + "," + Start.Y + "," + Start.Z +
                      " To " + Goal.X + "," + Goal.Y + "," + Goal.Z + " ; Mode " + Mode.ID);

            Timer.Start();
        }

        private void AddStartPart(int angle)
        {
            var part = new Rail(this, null, Types.RailStraight, Start.X, Start.Y, Start.Z, angle);
            var estimate = CostEstimate(part);
            Heap.Add(estimate + part.Cost, part);
            MaxEstimate = estimate * 5;
            _bestCost = estimate;
            _bestPath = part;

            if (Mode.ID == 0 && estimate < 40)
            {
                Mode = PathingMode.Modes[1];
            }
        }

        public float CostEstimate(IElement part)
        {
            return (float) Math.Sqrt(Math.Pow(part.EndX - Goal.X, 2) + Math.Pow(part.EndY - Goal.Y, 2) +
                                     2 * Math.Pow(part.EndZ - Goal.Z, 2));
        }

        public void Step()
        {
            if (Heap.Count == 0)
            {
                if (!Autocomplete()) Fail();
                return;
            }

            Counter++;
            var current = Heap.First();
//            if (current.Key > MaxEstimate)
//            {
//                Fail();
//                if (Mode.ID < 2)
//                {
//                    GameControl.Paths.CurrentJob = new PathingJob { Goal = Goal, Start = Start, Mode = PathingMode.Modes[Mode.ID + 1], StartDirection = StartDirection, GoalDirection = GoalDirection };
//                }
//                return;
//            }

            if (CheckGoalReached(current.Value))
            {
                Finish(current.Value);
                return;
            }

            var currentEstimate = CostEstimate(current.Value);
            if (currentEstimate < _bestCost)
            {
                _bestCost = currentEstimate;
                _bestPath = current.Value;
            }

//            if (Timer.ElapsedMilliseconds > 2000 && currentEstimate < MaxEstimate/20)
//            {
//                if (Autocomplete()) return;
//            }
            else if (Timer.ElapsedMilliseconds > 5000)
            {
                if( Autocomplete() ) return;
            }

            Heap.RemoveAt(0);

            foreach (var part in current.Value.PossibleNext())
            {
                if (!part.IsPossible()) continue;
                if (!CheckClosedSet(part)) continue;
                if (CostEstimate(part) > MaxEstimate/2) continue;

                var cost = CostEstimate(part) + part.Cost;
                Heap.Add(cost, part);
            }
        }

        private bool Autocomplete()
        {
            Debug.Log("Autocomplete Job ID: " + ID + " Time: " + Timer.ElapsedMilliseconds);
            var autocomplete = GameControl.Paths.AutoComplete;
            autocomplete.SetStartPosition(_bestPath);
            autocomplete.SetGoalPosition(Goal, GoalDirection);
            var part = autocomplete.Autocomplete();
            if (part != null && CheckGoalReached(part))
            {
                Finish(part);
                return true;
            }
            _bestCost = MaxEstimate;
            _bestPath = Heap.First().Value;
            return false;
        }

        private bool CheckClosedSet(IElement part)
        {
            var key = part.EndX + "," + part.EndY + "," + part.Type.ID;
            if (Mode.MatchZ) key += "," + part.EndZ;

            if (ClosedSet.ContainsKey(key))
            {
                if (!Mode.UpdateClosedSet) return false;
                if (ClosedSet[key].Cost <= part.Cost) return false;
                ClosedSet[key] = part;
                return true;
            }
            ClosedSet.Add(key, part);
            return true;
        }

        private void Finish(IElement finalPart)
        {
            Debug.Log("Job ID: " + ID + " finished Time: " + Timer.ElapsedMilliseconds);
            Done = true;
            Success = true;

            var path = new List<IElement>();
            var current = finalPart;
            while (current != null)
            {
                path.Add(current);
                if(Mode.ID > 2)current.Show();
                current = current.Previous;
            }

            path.Reverse();

            Timer.Reset();

            if (Mode.ID <= 2)
            {
                var optimalLength = Mode.OptimalLength;
                var count = (int) Math.Floor((float) path.Count/optimalLength);
                if (count == 0) count = 1;
                var firstIndex = 0;
                var n = 0;

                while (firstIndex < path.Count)
                {
                    n++;
                    var firstPart = path[firstIndex];
                    var secondIndex = Math.Min((int) Math.Ceiling((float) path.Count / count * n), path.Count - 1);
                    var secondPart = path[secondIndex];

                    var startTile = GameControl.Map.GetTile(firstPart.X, firstPart.Y);
                    var goalTile = GameControl.Map.GetTile(secondPart.EndX, secondPart.EndY);
                    var startZ = Mode.MatchZ && startTile.AverageHeight() < 0 ? 1 : startTile.AverageHeight();
                    var goalZ = Mode.MatchZ && goalTile.AverageHeight() < 0 ? 1 : goalTile.AverageHeight();
                    var start = new Point3<int, float> { X = startTile.X, Y = startTile.Y, Z = startZ };
                    var goal = new Point3<int, float> { X = goalTile.X, Y = goalTile.Y, Z = goalZ };
                    var startDir = firstPart.Direction;
                    var goalDir = secondPart.EndDirection;
                    var job = new PathingJob { Goal = goal, Start = start, Mode = PathingMode.Modes[Mode.ID + 1], StartDirection = startDir, GoalDirection = goalDir };
                    GameControl.Paths.Jobs.Enqueue(job);

                    firstIndex = secondIndex + 1;
                }
            }
        }

        private void Fail()
        {
            Debug.Log("Job ID: " + ID + " failed Time: " + Timer.ElapsedMilliseconds);
            Done = true;
            Success = false;
        }

        private bool CheckGoalReached(IElement part)
        {
            if (Mode.MatchZ && Math.Abs(part.EndZ - Goal.Z) > 0.2f) return false;
            if (Mode.UseGoalDirection && part.EndDirection != GoalDirection) return false;
            return part.EndX == Goal.X && part.EndY == Goal.Y;
        }
    }
}