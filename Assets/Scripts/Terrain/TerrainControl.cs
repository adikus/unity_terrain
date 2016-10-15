using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Map;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Terrain
{
    public class TerrainControl
    {
        public Vector3 Offset;

        public int XChunks;
        public int YChunks;

        private TileTexture _texture;

        private const int ChunkSize = 50;

        private readonly GameObject[] _terrainObjects;

        public TerrainControl()
        {
            var map = GameControl.Map;

            var waterPlane = GameObject.Find("WaterProDaytime");
            var terrainPrefab = (GameObject) Resources.Load("prefabs/Terrain", typeof(GameObject));
            var cityPrefab = (GameObject) Resources.Load("prefabs/TempCityPrefab", typeof(GameObject));
            var townPrefab = (GameObject) Resources.Load("prefabs/TempTownPrefab", typeof(GameObject));

            waterPlane.transform.localScale = new Vector3((float) map.Width / 10, 1, (float) map.Height / 10);

            _texture = new TileTexture(64, map.Colors);

            Offset = new Vector3(-(float) map.Width / 2, 0, -(float) map.Height / 2);

            XChunks = (int) Math.Ceiling((float) map.Width / ChunkSize);
            YChunks = (int) Math.Ceiling((float) map.Height / ChunkSize);
            _terrainObjects = new GameObject[XChunks * YChunks];

            Debug.Log("Chunks: " + XChunks + " x " + YChunks);

            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();
            for (var i = 0; i < map.Width; i += ChunkSize)
            {
                for (var j = 0; j < map.Height; j += ChunkSize)
                {
                    var terrainObject = Object.Instantiate(terrainPrefab);
                    _texture.AttachTextureTo(terrainObject.GetComponent<Renderer>().material);
                    _terrainObjects[TerrainIndex(i, j)] = terrainObject;
                    UpdateTerrain(terrainObject, i, Math.Min(i + ChunkSize - 1, map.Width - 1), j, Math.Min(j + ChunkSize - 1, map.Height - 1));
                }
            }
            stopwatch.Stop();
            Debug.Log("UpdateTerrain: " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Reset();

            var cubeOffset = new Vector3(0.5f, 0.5f, 0.5f);

            foreach (var city in map.Objects.Cities)
            {
                var tile = map.GetTile(city.X, city.Y);
                var cityObject = Object.Instantiate(cityPrefab);
                cityObject.transform.position = Offset + cubeOffset + new Vector3(city.X, tile.AverageHeight()/2, city.Y);
            }

            foreach (var town in map.Objects.Towns)
            {
                var tile = map.GetTile(town.X, town.Y);
                var townObject = Object.Instantiate(townPrefab);
                townObject.transform.position = Offset + cubeOffset + new Vector3(town.X, tile.AverageHeight()/2, town.Y);
            }

            GameControl.Paths.Test();
        }

        private void UpdateTerrain(GameObject terrainObject, int x1, int x2, int y1, int y2)
        {
            var filter = terrainObject.GetComponent<MeshFilter>();
            var mesh = filter.mesh;
            mesh.Clear();

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            var triangleCounter = 0;
            for (var i = x1; i <= x2; i++)
            {
                for (var j = y1; j <= y2; j++)
                {
                    var tile = GameControl.Map.Tiles[i, j];

                    vertices.AddRange(tile.Vertices.Select(v => v + Offset));
                    uvs.AddRange(tile.UVs);
                    triangles.AddRange(tile.Triangles.Select(t => t + triangleCounter));

                    triangleCounter += tile.Vertices.Count;
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            terrainObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        // Update is called once per frame
        void Update () {

        }

        internal int TerrainIndex(int x, int y)
        {
            return x / ChunkSize * YChunks + y / ChunkSize;
        }

        internal Vector2 TerrainPosition(int index)
        {
            return new Vector2(index / YChunks * ChunkSize, index % YChunks * ChunkSize);
        }

        public void SetTileHeights(Tile tile, float nw, float ne, float sw, float se)
        {
            var map = GameControl.Map;

            tile.UpdateHeights(nw, ne, sw, se, true);

            var terrainIndexes = new HashSet<int>();

            for (var i = tile.X - 1; i <= tile.X + 1; i++)
            {
                for (var j = tile.Y - 1; j <= tile.Y + 1; j++)
                {
                    var t = map.GetTile(i, j);
                    if (t == null || t.Dummy()) continue;
                    t.ComputeMesh();
                    terrainIndexes.Add(TerrainIndex(i, j));
                }
            }

            foreach (var index in terrainIndexes)
            {
                var position = TerrainPosition(index);
                var x1 = (int) position.x;
                var y1 = (int) position.y;
                var x2 = Math.Min(x1 + ChunkSize - 1, map.Width - 1);
                var y2 = Math.Min(y1 + ChunkSize - 1, map.Height - 1);

                UpdateTerrain(_terrainObjects[index], x1, x2, y1, y2);
            }
        }
    }
}
