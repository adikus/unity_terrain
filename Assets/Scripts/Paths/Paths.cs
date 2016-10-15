using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts.Map.Utils;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Paths
{
    public class Paths : MonoBehaviour {

        private GameObject _straightPrefab;
        private GameObject _curve902Prefab;

        void Start()
        {
            _straightPrefab = (GameObject) Resources.Load("prefabs/Paths/PathStraight", typeof(GameObject));
            _curve902Prefab = (GameObject) Resources.Load("prefabs/Paths/Path-90-2", typeof(GameObject));
        }

        // Update is called once per frame
        void Update ()
        {
            if (Heap == null) return;
            //if (!Input.GetKey(KeyCode.P)) return;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (Heap.Count > 0)
            {
                GameControl.Control.UI.DebugLines[2] = "Cost: " + (int) CostEstimate(Heap.First().Value);
            } else if (Jobs.Count > 0)
            {
                StartNextJob();
            }

            while (stopwatch.ElapsedMilliseconds < 1000.0/60)
            {
                if (Heap.Count == 0) return;
                var current = Heap.First();

                if (current.Key > MaxEstimate)
                {
                    Debug.Log("Job failed");
                    Heap.Clear();
    //	            foreach (var tile in Set)
    //	            {
    //	                FailedSet[tile.Key] = tile.Value;
    //	            }
                    Set.Clear();
                    foreach (var part in Parts)
                    {
                        part.Hide();
                    }
                    Parts.Clear();
                    if (Mode < 2)
                    {
                        CurrentJob.Mode += 1;
                        StartJob(CurrentJob);
                    }
                    break;
                }

                if (current.Value.EndX == Goal.X && current.Value.EndY == Goal.Y)
                {
                    if (Mode < 1 || GoalDirection < 0 || current.Value.EndDirection == GoalDirection)
                    {
                        if (Mode < 2 || Math.Abs(current.Value.Z - Goal.Z) < 0.5f)
                        {
                            Heap.Clear();
                            FailedSet = new Dictionary<string, PathPart>(Set);
                            Set.Clear();
                            FailedSet.Clear();
                            ShowPath(current.Value);
                            return;
                        }
                    }
                }

                current.Value.Hide();
                Heap.RemoveAt(0);
                for (var i = 0; i < 5; i++)
                {
                    var minZ = Mode <= 1 ? 0 : -0.2f;
                    var maxZ = Mode <= 1 ? 0 : 0.2f;
                    for (var z = minZ; z <= maxZ; z += 0.2f)
                    {
                        var part = Mode <= 1 ? AddPart(current.Value, i, null) : AddPart(current.Value, i, z);
                        if (part.IsPossible())
                        {
                            var key = part.X + "," + part.Y + "," + part.Type;
                            //if (Mode > 0) key += "," + part.Direction;
                            if (Mode > 1) key += "," + part.Z;
                            //if (FailedSet.ContainsKey(key) && part.Cost > 20) continue;
                            if (Set.ContainsKey(key))
                            {
                                if (Mode == 0)continue;
                                var existingPart = Set[key];
                                if (existingPart.Cost > part.Cost)
                                {
                                    Set[key] = part;
                                }
                                else //if(existingPart.Cost * 1.2 > part.Cost)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                Set.Add(key, part);
                            }
                            //if(Mode > 0)part.Show();
                            Parts.Add(part);
                            var cost = CostEstimate(part) + part.Cost;
                            Heap.Add(cost, part);
                        }
                        else
                        {
                            part.Destroy();
                        }
                    }
                }
            }

            stopwatch.Stop();
        }

        public List<PathPart> Parts;
        public Point3<int,float> Goal;
        public int GoalDirection;
        public SortedList<float, PathPart> Heap;
        public Dictionary<string, PathPart> Set;
        public Dictionary<string, PathPart> FailedSet;
        public Stopwatch Timer;
        public int Mode;
        public float MaxEstimate;

        public Queue<PathingJob> Jobs;
        public PathingJob CurrentJob;

        public class CostComparer : IComparer<float>
        {
            public int Compare(float i1, float i2)
            {
                return i1 < i2 ? -1 : 1;
            }
        }

        public void Test()
        {
            Heap = new SortedList<float, PathPart>(new CostComparer());
            Set = new Dictionary<string, PathPart>();
            FailedSet = new Dictionary<string, PathPart>();
            Parts = new List<PathPart>();
            Timer = new Stopwatch();
            Jobs = new Queue<PathingJob>();

            // Get Minimum spanning tree
            var minEdges = new Point2<int>[GameControl.Control.Map.Cities.Count];
            var unconnectedVertices = new Dictionary<int, float>();
            var forestEdges = new List<Point2<int>>();

            for (var i = 0; i < minEdges.Length; i++)
            {
                minEdges[i] = new Point2<int>{X=-1,Y=-1};
                unconnectedVertices[i] = float.MaxValue;
            }

            var nextNode = unconnectedVertices.Keys.First();
            while (unconnectedVertices.Count > 0)
            {
                var node = nextNode;
                unconnectedVertices.Remove(node);
                if (minEdges[node].X > -1)
                {
                    var edge = minEdges[node];
                    forestEdges.Add(edge);
                    //Debug.Log("Connecting: " + edge.X + " To: " + edge.Y + " Cost: " + node);
                }

                var min = float.MaxValue;
                foreach (var key in new List<int> (unconnectedVertices.Keys))
                {
                    var cityA = GameControl.Control.Map.Cities[node];
                    var cityB = GameControl.Control.Map.Cities[key];
                    var cost = Math.Sqrt(Math.Pow(cityA.X - cityB.X, 2) + Math.Pow(cityA.Y - cityB.Y, 2));
                    if (cost < unconnectedVertices[key])
                    {
                        minEdges[key] = new Point2<int>{X=node,Y=key};
                        unconnectedVertices[key] = (float) cost;
                    }
                    if (Math.Min(cost, unconnectedVertices[key]) < min)
                    {
                        min = (float) Math.Min(cost, unconnectedVertices[key]);
                        nextNode = key;
                    }
                }
            }

            foreach (var edge in forestEdges)
            {
                var cityA = GameControl.Control.Map.Cities[edge.X];
                var cityB = GameControl.Control.Map.Cities[edge.Y];
                var startTile = GameControl.Control.Map.GetTile(cityA.X, cityA.Y);
                var goalTile = GameControl.Control.Map.GetTile(cityB.X, cityB.Y);
                var start = new Point3<int, float> { X = startTile.X, Y = startTile.Y, Z = startTile.AverageHeight() };
                var goal = new Point3<int, float> { X = goalTile.X, Y = goalTile.Y, Z = goalTile.AverageHeight() };
                var job = new PathingJob { Goal = goal, Start = start, Mode = 0 };
                Jobs.Enqueue(job);
            }
            Debug.Log("Added " + Jobs.Count + " jobs");
        }

        public void StartNextJob()
        {
            var job = Jobs.Dequeue();
            StartJob(job);
        }

        public void StartJob(PathingJob job)
        {
            Goal = job.Goal;
            GoalDirection = job.GoalDirection ?? -1;
            var minA = job.StartDirection ?? 0;
            var maxA = job.StartDirection ?? 360;
            for (var a = minA; a <= maxA; a += 90)
            {
                var part = new PathPart(this, null, 0, job.Start.X, job.Start.Y, job.Start.Z, a);
                Parts.Add(part);
                Heap.Add(CostEstimate(part) + part.Cost, part);
                MaxEstimate = CostEstimate(part) * 5;

                if (job.Mode == 0 && CostEstimate(part) < 40)
                {
                    job.Mode = 1;
                }
            }

            GameControl.Control.UI.DebugLines[1] = "Current job: From " + job.Start.X + "," + job.Start.Y + "," +
                                                   job.Start.Z + " To " + job.Goal.X + "," + job.Goal.Y + "," +
                                                   job.Start.Z + " ; Mode " + job.Mode;
            Debug.Log("Starting Pathing Job From "+job.Start.X+","+job.Start.Y+","+job.Start.Z+" To "+job.Goal.X+","+job.Goal.Y+","+job.Start.Z+" ; Mode " + job.Mode);

            Timer.Start();
            Mode = job.Mode;
            CurrentJob = job;
        }

        public void ShowPath(PathPart finalPart)
        {
            foreach (var part in Parts)
            {
                part.Hide();
            }
            Parts.Clear();

            var path = new List<PathPart>();
            var current = finalPart;
            while (current != null)
            {
                path.Add(current);
                if(Mode > 2)current.Show();
                current = current.Previous;
            }

            path.Reverse();

            Debug.Log("Path: " + Timer.ElapsedMilliseconds);
            Timer.Reset();

            if (Mode <= 2)
            {
                var optimalLength = Mode == 0 ? 60 : (Mode == 1 ? 25 : 10);
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

                    var startTile = GameControl.Control.Map.GetTile(firstPart.X, firstPart.Y);
                    var goalTile = GameControl.Control.Map.GetTile(secondPart.EndX, secondPart.EndY);
                    var startZ = Mode == 1 && startTile.AverageHeight() < 0 ? 1 : startTile.AverageHeight();
                    var goalZ = Mode == 1 && goalTile.AverageHeight() < 0 ? 1 : goalTile.AverageHeight();
                    var start = new Point3<int, float> { X = startTile.X, Y = startTile.Y, Z = startZ };
                    var goal = new Point3<int, float> { X = goalTile.X, Y = goalTile.Y, Z = goalZ };
                    var startDir = firstPart.Direction;
                    var goalDir = secondPart.EndDirection;
                    var job = new PathingJob { Goal = goal, Start = start, Mode = Mode + 1, StartDirection = startDir, GoalDirection = goalDir };
                    Jobs.Enqueue(job);

                    firstIndex = secondIndex + 1;
                }
            }
        }

        public float CostEstimate(PathPart part)
        {
            return
                (float)
                Math.Sqrt(Math.Pow(part.EndX - Goal.X, 2) + Math.Pow(part.EndY - Goal.Y, 2) +
                          2 * Math.Pow(part.Z - Goal.Z, 2));
        }

        public PathPart AddPart(int type, float? z)
        {
            var lastPart = Parts.Last();
            return new PathPart(this, lastPart, type, lastPart.EndX, lastPart.EndY, lastPart.Z + z, lastPart.EndDirection);
        }

        public PathPart AddPart(PathPart previous, int type, float? z)
        {
            return new PathPart(this, previous, type, previous.EndX, previous.EndY, previous.Z + z, previous.EndDirection);
        }

        public GameObject InstantiateType(int type)
        {
            switch (type)
            {
                case 0:
                    return Object.Instantiate(_straightPrefab);
                case 1:
                    return Object.Instantiate(_curve902Prefab);
                case 2:
                    var part = Object.Instantiate(_curve902Prefab);
                    part.transform.localScale = Vector3.Scale(part.transform.localScale, new Vector3(1, 1, -1));
                    return part;
                case 3:
                    return Object.Instantiate(_curve902Prefab);
                default:
                    var part2 = Object.Instantiate(_curve902Prefab);
                    part2.transform.localScale = Vector3.Scale(part2.transform.localScale, new Vector3(1, 1, -1));
                    return part2;
            }
        }
    }

    public class PathPart
    {
        public int Direction;
        public int X;
        public int Y;
        public float Z;
        public int Type;

        public int EndX;
        public int EndY;
        public int EndDirection;

        public float Cost;

        private Paths _pathController;
        private GameObject _object;

        public PathPart Previous;

        public PathPart(Paths pathController, PathPart previous, int type, int x, int y, float? z, int direction)
        {
            _pathController = pathController;

            Previous = previous;
            Type = type;
            X = x;
            Y = y;
            if (z == null)
            {
                var map = GameControl.Control.Map;
                var tile = map.GetTile(X, Y);
                Z = tile.AverageHeight();
            }
            else
            {
                Z = z ?? 0;
            }
            Direction = direction;

            var path = GetTypeVector();
            path = Quaternion.AngleAxis(Direction, Vector3.up) * path;
            var end = path + new Vector3(X, 0, Y);
            EndX = (int) Math.Round(end.x);
            EndY = (int) Math.Round(end.z);
            var angle = GetTypeAngle();
            EndDirection = Direction + angle;

            Cost = (Previous != null ? Previous.Cost : 0) + PartCost();
        }

        private Vector3 GetTypeVector()
        {
            if (Type == 0) return new Vector3(1, 0, 0);
            if (Type == 1) return new Vector3(1, 0, -2);
            if (Type == 2) return new Vector3(1, 0, 2);
            if (Type == 3) return new Vector3(2, 0, -1);
            return new Vector3(2, 0, 1);
        }

        private int GetTypeAngle()
        {
            if (Type == 0) return 0;
            if (Type == 1) return 90;
            if (Type == 2) return -90;
            if (Type == 3) return 0;
            return 0;
        }

        private float GetTypeBaseCost()
        {
            if (Type == 0) return 1;
            if (Type == 1) return 4;
            if (Type == 2) return 4;
            if (Type == 3) return 2.5f;
            return 2.5f;
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(_object);
        }

        public void Hide()
        {
            if (_object == null) return;
            UnityEngine.Object.Destroy(_object);
        }

        public void Show()
        {
            if (_object != null) return;
            _object = _pathController.InstantiateType(Type);

            var cubeOffset = new Vector3(0.5f, 0.125f, 0.5f);

            _object.transform.position = GameControl.Control.Terrain.Offset + cubeOffset + new Vector3(X, Z/2, Y);
            _object.transform.Rotate(Vector3.up, Direction);

            //var cost = (_pathController.CostEstimate(this) + Cost) % 50 / 50;
            //var color = new Color(cost, cost, cost);
            var color = _pathController.Mode == 0 ? Color.red : (_pathController.Mode == 1 ? Color.blue : (_pathController.Mode == 2 ? Color.green : Color.yellow));

            if (_object.transform.childCount > 0)
            {
                foreach (Transform child in _object.transform)
                {
                    child.GetComponent<Renderer>().material.color = color;//Type == 1 ? Color.blue : Color.green;
                }
            }
            else
            {
                _object.GetComponent<Renderer>().material.color = color;//Color.red;
            }
        }

        public bool IsPossible()
        {
            var map = GameControl.Control.Map;
            var endTile = map.GetTile(EndX, EndY);
            if (endTile.Dummy()) return false;
            if (Z < -10) return false;
            if (endTile.AverageHeight() < 0 && Type > 0) return false;
            if (_pathController.Mode >= 2)
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
            var map = GameControl.Control.Map;
            var tile = map.GetTile(X, Y);
            var endTile = map.GetTile(EndX, EndY);
            var baseCost = GetTypeBaseCost();
            var heightDiffCost = Previous == null ? 0 : Math.Abs(Z - Previous.Z);
            var heightCost = Math.Abs(Z - tile.AverageHeight());
            heightCost += Math.Abs(Z - endTile.AverageHeight());
            var waterCost = Z < 0 ? -Z : 0;
            if (_pathController.Mode < 2) heightDiffCost *= 5;
            if (_pathController.Mode == 3) baseCost *= 3;
            var finalCost = baseCost + 5*heightDiffCost + heightCost + waterCost;
            if (_pathController.Mode >= 2) finalCost *= 0.5f;
            return finalCost;
        }
    }

    public class PathingJob
    {
        public Point3<int, float> Start { get; set; }
        public Point3<int, float> Goal { get; set; }
        public int Mode;
        public int? StartDirection;
        public int? GoalDirection;
    }
}