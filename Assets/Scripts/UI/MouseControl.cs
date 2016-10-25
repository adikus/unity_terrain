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
            _terrainOverlay.transform.localScale = _terrainOverlay.transform.localScale * GameControl.Terrain.Scale;
        }

        public void Update()
        {
            var tile = GetMouseTile();
            if (tile == null || tile.Dummy()) return;

            GameControl.UI.DebugLines[0] = "Tile: " + tile.X + ", " + tile.Y + ", Height: " + tile.NW.y;

            UpdateOverlayMesh(tile);

//            if (Input.GetKeyDown(KeyCode.N))
//            {
//                GameControl.Paths.AutoComplete.Angle += 90;
//                if (GameControl.Paths.AutoComplete.Angle > 270) GameControl.Paths.AutoComplete.Angle = 0;
//                GameControl.UI.DebugLines[6] = "Angle: " + GameControl.Paths.AutoComplete.Angle + " DA: " + GameControl.Paths.AutoComplete.DA;
//            }
//
//            if (Input.GetKeyDown(KeyCode.M))
//            {
//                GameControl.Paths.AutoComplete.DA += 90;
//                if (GameControl.Paths.AutoComplete.DA > 180) GameControl.Paths.AutoComplete.DA = -90;
//                GameControl.UI.DebugLines[6] = "Angle: " + GameControl.Paths.AutoComplete.Angle + " DA: " + GameControl.Paths.AutoComplete.DA;
//            }

            if (Input.GetMouseButtonDown(0))
            {
                //GameControl.Paths.AutoComplete.SetStartPosition(tile);
            }

            if (Input.GetMouseButtonDown(1))
            {
                //GameControl.Paths.AutoComplete.SetGoalPosition(tile);

//                var heightList = new List<float>(new [] { tile.NW.y, tile.NE.y, tile.SW.y, tile.SE.y }) ;
//                var min = heightList.Min();
//                GameControl.Terrain.SetTileHeights(tile, min, min, min, min);
            }
        }

        internal Vector2? GetMouseCoords()
        {
            const int layerMask = 1 << 8;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, 2000 * GameControl.Terrain.Scale, layerMask)) return null;
            var x = (int) (hit.point.x - GameControl.Terrain.Scale * GameControl.Terrain.Offset.x);
            var y = (int) (hit.point.z - GameControl.Terrain.Scale * GameControl.Terrain.Offset.z);
            return new Vector2(x / GameControl.Terrain.Scale, y / GameControl.Terrain.Scale);
        }

        internal Tile GetMouseTile()
        {
            var mouseCoords = GetMouseCoords();
            if (mouseCoords == null || GameControl.Map == null) return null;
            var coords = mouseCoords.Value;
            return GameControl.Map.GetTile((int) coords.x, (int) coords.y);
        }

        private void UpdateOverlayMesh(Tile tile)
        {
            var filter = _terrainOverlay.GetComponent<MeshFilter>();
            var mesh = filter.mesh;

            mesh.Clear();

            var vertices = tile.Vertices.Select(v => v + GameControl.Terrain.Offset);
            var uvs = tile.UVs;
            var triangles = tile.Triangles;

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
        }
    }
}