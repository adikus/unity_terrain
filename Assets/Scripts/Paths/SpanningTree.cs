using System;
using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.Map.Utils;

namespace Assets.Scripts.Paths
{
    public class SpanningTree
    {
        public static void ConnectPlaces(List<Point2<int>> places)
        {
            var minEdges = new Point2<int>[places.Count];
            var unconnectedVertices = new Dictionary<int, float>();
            var forestEdges = new List<Point2<int>>();

            for (var i = 0; i < minEdges.Length; i++)
            {
                minEdges[i] = new Point2<int> { X=-1, Y=-1 };
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
                }

                var min = float.MaxValue;
                foreach (var key in new List<int> (unconnectedVertices.Keys))
                {
                    var placeA = places[node];
                    var placeB = places[key];
                    var cost = Cost(placeA, placeB);
                    if (cost < unconnectedVertices[key])
                    {
                        minEdges[key] = new Point2<int> { X=node, Y=key };
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
                var placeA = places[edge.X];
                var placeB = places[edge.Y];
                var startTile = GameControl.Map.GetTile(placeA.X, placeA.Y);
                var goalTile = GameControl.Map.GetTile(placeB.X, placeB.Y);
                var start = new Point3<int, float> { X = startTile.X, Y = startTile.Y, Z = startTile.AverageHeight() };
                var goal = new Point3<int, float> { X = goalTile.X, Y = goalTile.Y, Z = goalTile.AverageHeight() };
                var job = new PathingJob { Goal = goal, Start = start, Mode = PathingMode.Modes[0] };
                GameControl.Paths.Jobs.Enqueue(job);
            }
        }

        private static double Cost(Point2<int> placeA, Point2<int> placeB)
        {
            return Math.Sqrt(Math.Pow(placeA.X - placeB.X, 2) + Math.Pow(placeA.Y - placeB.Y, 2));
        }
    }
}