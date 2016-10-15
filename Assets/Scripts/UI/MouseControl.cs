using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Assets.Scripts.Map;

namespace Assets.Scripts.UI
{
    public class MouseControl
    {
        private readonly GameObject _terrainOverlay;

        public MouseControl()
        {
            var terrainOverlayPrefab = (GameObject) Resources.Load("prefabs/TerrainOverlay", typeof(GameObject));
            _terrainOverlay = Object.Instantiate(terrainOverlayPrefab);
        }

        public void Update()
        {
            var tile = GetMouseTile();
            if (tile == null || tile.Dummy()) return;

            GameControl.Control.UI.DebugLines[0] = "Tile: " + tile.X + ", " + tile.Y + ", Height: " + tile.NW.y;

            UpdateOverlayMesh(tile);

            if (Input.GetMouseButtonDown(1))
            {
                var heightList = new List<float>(new [] { tile.NW.y, tile.NE.y, tile.SW.y, tile.SE.y }) ;
                var min = heightList.Min();
                GameControl.Control.Terrain.SetTileHeights(tile, min, min, min, min);
            }
        }

        internal Vector2? GetMouseCoords()
        {
            const int layerMask = 1 << 8;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 2000, layerMask)) return null;
            var x = (int) (hit.point.x - GameControl.Control.Terrain.Offset.x);
            var y = (int) (hit.point.z - GameControl.Control.Terrain.Offset.z);
            return new Vector2(x, y);
        }

        internal Tile GetMouseTile()
        {
            var mouseCoords = GetMouseCoords();
            if (mouseCoords == null || GameControl.Control.Map == null) return null;
            var coords = mouseCoords.Value;
            return GameControl.Control.Map.GetTile((int) coords.x, (int) coords.y);
        }

        private void UpdateOverlayMesh(Tile tile)
        {
            var filter = _terrainOverlay.GetComponent<MeshFilter>();
            var mesh = filter.mesh;

            mesh.Clear();

            var vertices = tile.Vertices.Select(v => v + GameControl.Control.Terrain.Offset);
            var uvs = tile.UVs;
            var triangles = tile.Triangles;

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
        }
    }
}