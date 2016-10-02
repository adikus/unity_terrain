using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Terrain
{
    public class Tile
    {
        public List<Vector3> Vertices;
        public List<Vector2> UVs;
        public List<int> Triangles;

        public Vector3 NW;
        public Vector3 NE;
        public Vector3 SW;
        public Vector3 SE;

        public int X;
        public int Y;

        private readonly Map _map;

        private float _colorSpacing;

        public Tile(Map map, int x, int y, float a, float b, float c, float d)
        {
            _map = map;

            X = x;
            Y = y;

//            b = a;
//            c = a;
//            d = a;

            NW = new Vector3(x, a, y);
            NE = new Vector3(x + 1, b, y);
            SW = new Vector3(x, d, y + 1);
            SE = new Vector3(x + 1, c, y + 1);
        }

        internal void ComputeMesh()
        {
            var numColors = _map.Colors.BorderedColors.Count + _map.Colors.BorderlessColors.Count;
            _colorSpacing = 1f / numColors;

            List<Vector3> triangle1;
            List<Vector3> triangle2;

            if (Math.Abs(NW.y - SE.y) < Math.Abs(NE.y - SW.y))
            {
                triangle1 = new List<Vector3> {NW, NE, SE};
                triangle2 = new List<Vector3> {NW, SE, SW};
            }
            else
            {
                triangle1 = new List<Vector3> {NW, NE, SW};
                triangle2 = new List<Vector3> {NE, SE, SW};
            }

            Vertices = new List<Vector3>();
            Vertices.AddRange(triangle1);
            Vertices.AddRange(triangle2);

            UVs = new List<Vector2>();
            AddTriangleUVs(X, Y, UVs, triangle1);
            AddTriangleUVs(X, Y, UVs, triangle2);

            Triangles = new List<int> {2, 1, 0, 5, 4, 3};

            var westTile = _map.GetTile(X - 1, Y);
            if (westTile.NE.y < NW.y || westTile.SE.y < SW.y)
            {
                AddUVsAndTrianglesForSide(new List<Vector3> {NW, SW, westTile.NE, westTile.SE});
            }

            var northTile = _map.GetTile(X, Y - 1);
            if (northTile.SW.y < NW.y || northTile.SE.y < NE.y)
            {
                AddUVsAndTrianglesForSide(new List<Vector3> {NE, NW, northTile.SE, northTile.SW});
            }

            var eastTile = _map.GetTile(X + 1, Y);
            if (eastTile.NW.y < NE.y || eastTile.SW.y < SE.y)
            {
                AddUVsAndTrianglesForSide(new List<Vector3> {SE, NE, eastTile.SW, eastTile.NW});
            }

            var southTile = _map.GetTile(X, Y + 1);
            if (southTile.NW.y < SW.y || southTile.NE.y < SE.y)
            {
                AddUVsAndTrianglesForSide(new List<Vector3> {SW, SE, southTile.NW, southTile.NE});
            }
        }

        private void AddUVsAndTrianglesForSide(List<Vector3> points)
        {
            var vertexCount = Vertices.Count;
            Vertices.AddRange(points);
            var colorIndex = _map.Colors.GetBorderlessIndex(0);
            UVs.AddRange(new List<Vector2>
            {
                new Vector2(_colorSpacing * (colorIndex + 0.25f), 0),
                new Vector2(_colorSpacing * (colorIndex + 0.75f), 0),
                new Vector2(_colorSpacing * (colorIndex + 0.25f), 1),
                new Vector2(_colorSpacing * (colorIndex + 0.75f), 1)
            });

            Triangles.AddRange(new List<int> {3, 1, 0, 2, 3, 0}.Select(i => i + vertexCount));
        }

        private void AddTriangleUVs(int x, int y, List<Vector2> uvs, List<Vector3> triangle)
        {
            foreach (var point in triangle)
            {
                var uvx = point.x - x;
                var uvy = point.z - y;

                var triangleHeight = triangle.Select(i => i.y).Max();
                var colorIndex = _map.Colors.GetColorIndex(triangleHeight);

                uvs.Add(new Vector2(_colorSpacing * (colorIndex + uvx), uvy));
            }
        }

        public new string ToString()
        {
            var vertices = string.Join(",", Vertices.Select(i => "(" + i.x + "," + i.y + "," + i.z + ")").ToArray());
            var uvs = string.Join(",", UVs.Select(i => "(" + i.x + "," + i.y + ")").ToArray());
            return "Vertices: " + vertices + "\nUVS: " + uvs;
        }

        public void UpdateHeights(float nw, float ne, float sw, float se, bool affectNeighbours)
        {
            NW.y = nw;
            NE.y = ne;
            SW.y = sw;
            SE.y = se;

            if (!affectNeighbours) return;

            var westTile = _map.GetTile(X - 1, Y);
            var northTile = _map.GetTile(X, Y - 1);
            var eastTile = _map.GetTile(X + 1, Y);
            var southTile = _map.GetTile(X, Y + 1);

            if (westTile != null && !westTile.Dummy())
            {
                westTile.NE.y = nw;
                westTile.SE.y = sw;
            }
            if (northTile != null && !northTile.Dummy())
            {
                northTile.SW.y = nw;
                northTile.SE.y = ne;
            }
            if (eastTile != null && !eastTile.Dummy())
            {
                eastTile.NW.y = ne;
                eastTile.SW.y = se;
            }
            if (southTile != null && !southTile.Dummy())
            {
                southTile.NW.y = sw;
                southTile.NE.y = se;
            }

            var northWestTile = _map.GetTile(X - 1, Y - 1);
            var northEastTile = _map.GetTile(X + 1, Y - 1);
            var southWestTile = _map.GetTile(X - 1, Y + 1);
            var southEastTile = _map.GetTile(X + 1, Y + 1);

            if (northWestTile != null && !northWestTile.Dummy())
            {
                northWestTile.SE.y = nw;
            }
            if (northEastTile != null && !northEastTile.Dummy())
            {
                northEastTile.SW.y = ne;
            }
            if (southWestTile != null && !southWestTile.Dummy())
            {
                southWestTile.NE.y = sw;
            }
            if (southEastTile != null && !southEastTile.Dummy())
            {
                southEastTile.NW.y = se;
            }
        }

        public bool Dummy()
        {
            return NW.y <= -100;
        }
    }
}