using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Terrain;

public class TerrainGeneration : MonoBehaviour
{
    public Map Map;
    public GameObject TerrainPrefab;
    public GameObject WaterPlane;

    public GameObject CityPrefab;
    public GameObject TownPrefab;

    public Vector3 Offset;

    public int XChunks;
    public int YChunks;

    private Terrain.Texture _texture;

    private const int ChunkSize = 50;

    private GameObject[] terrainObjects;

    public void Awake()
    {
        const int width = 200;
        const int height = 200;
        const int landPercentage = 90;
        const string seed = "Test Seed 1";

        WaterPlane.transform.localScale = new Vector3((float) width / 10, 1, (float) height / 10);

        Map = new Map(width, height, landPercentage, seed);
//        for (var i = 21; i <= 50; i++)
//        {
//            Map = new Map(width, height, landPercentage, "Test Seed "+i);
//        }
    }

    public void Start()
    {
        _texture = new Terrain.Texture(64, Map.Colors);

        Offset = new Vector3(-(float) Map.Width / 2, 0, -(float) Map.Height / 2);

        XChunks = (int) Math.Ceiling((float) Map.Width / ChunkSize);
        YChunks = (int) Math.Ceiling((float) Map.Height / ChunkSize);
        terrainObjects = new GameObject[XChunks * YChunks];

        Debug.Log("Chunks: " + XChunks + " x " + YChunks);

        var stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        for (var i = 0; i < Map.Width; i += ChunkSize)
        {
            for (var j = 0; j < Map.Height; j += ChunkSize)
            {
                var terrainObject = Instantiate(TerrainPrefab);
                _texture.AttachTextureTo(terrainObject.GetComponent<Renderer>().material);
                terrainObjects[TerrainIndex(i, j)] = terrainObject;
                UpdateTerrain(terrainObject, i, Math.Min(i + ChunkSize - 1, Map.Width - 1), j, Math.Min(j + ChunkSize - 1, Map.Height - 1));
            }
        }
        stopwatch.Stop();
        Debug.Log("UpdateTerrain: " + stopwatch.ElapsedMilliseconds + " ms");
        stopwatch.Reset();

        var cubeOffset = new Vector3(0.5f, 0.5f, 0.5f);

        foreach (var city in Map.Cities)
        {
            var tile = Map.GetTile((int) city.x, (int) city.y);
            var cityObject = Instantiate(CityPrefab);
            cityObject.transform.position = Offset + cubeOffset + new Vector3(city.x, tile.AverageHeight()/2, city.y);
        }

        foreach (var town in Map.Towns)
        {
            var tile = Map.GetTile((int) town.x, (int) town.y);
            var townObject = Instantiate(TownPrefab);
            townObject.transform.position = Offset + cubeOffset + new Vector3(town.x, tile.AverageHeight()/2, town.y);
        }
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
                var tile = Map.Tiles[i, j];

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
        tile.UpdateHeights(nw, ne, sw, se, true);

        var terrainIndexes = new HashSet<int>();

        for (var i = tile.X - 1; i <= tile.X + 1; i++)
        {
            for (var j = tile.Y - 1; j <= tile.Y + 1; j++)
            {
                var t = Map.GetTile(i, j);
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
            var x2 = Math.Min(x1 + ChunkSize - 1, Map.Width - 1);
            var y2 = Math.Min(y1 + ChunkSize - 1, Map.Height - 1);

            UpdateTerrain(terrainObjects[index], x1, x2, y1, y2);
        }
    }
}
