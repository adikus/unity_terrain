using System.Collections.Generic;
using Terrain;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MouseControl : MonoBehaviour {

    public GameObject DebugText;
    public GameObject TerrainController;
    public GameObject TerrainOverlayPrefab;

    private Text _debugText;
    private GameObject _terrainOverlay;
    private TerrainGeneration _terrainGeneration;
    private Map _map;

    // Use this for initialization
	void Start ()
	{
	    _debugText = DebugText.GetComponent<Text>();
	    _terrainOverlay = Instantiate(TerrainOverlayPrefab);
	    _terrainGeneration = TerrainController.GetComponent<TerrainGeneration>();
	    _map = _terrainGeneration.Map;
	}
	
	// Update is called once per frame
	void Update ()
	{
	    var tile = GetMouseTile();
	    if (tile == null || tile.Dummy()) return;

	    _debugText.text = "Tile: " + tile.X + ", " + tile.Y + ", Height: " + tile.NW.y;
	    UpdateOverlayMesh(tile);

	    if (Input.GetMouseButtonDown(1))
	    {
	        var heightList = new List<float>(new [] { tile.NW.y, tile.NE.y, tile.SW.y, tile.SE.y }) ;
	        var min = heightList.Min();
	        _terrainGeneration.SetTileHeights(tile, min, min, min, min);
	    }
	}

    private void UpdateOverlayMesh(Tile tile)
    {
        var filter = _terrainOverlay.GetComponent<MeshFilter>();
        var mesh = filter.mesh;

        mesh.Clear();

        var vertices = tile.Vertices.Select(v => v + _terrainGeneration.Offset);
        var uvs = tile.UVs;
        var triangles = tile.Triangles;

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
    }

    internal Vector2? GetMouseCoords()
    {
        const int layerMask = 1 << 8;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 2000, layerMask)) return null;
        var x = (int) (hit.point.x - _terrainGeneration.Offset.x);
        var y = (int) (hit.point.z - _terrainGeneration.Offset.z);
        return new Vector2(x, y);
    }

    internal Tile GetMouseTile()
    {
        var mouseCoords = GetMouseCoords();
        if (mouseCoords == null) return null;
        var coords = mouseCoords.Value;
        if (_map == null) return null;
        return _map.GetTile((int) coords.x, (int) coords.y);
    }
}
