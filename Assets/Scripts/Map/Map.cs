using Assets.Scripts.Map.Objects;
using Assets.Scripts.Utils;

namespace Assets.Scripts.Map
{
    public class Map
    {
        public Colors Colors;
        public Grid Grid;
        public Tile[,] Tiles;
        public MapObjects Objects;

        public readonly int Width;
        public readonly int Height;
        public readonly float LandPercentage;
        public readonly string Seed;

        public Map(int width , int height, int landPercentage, string seed)
        {
            Width = width;
            Height = height;
            LandPercentage = landPercentage;
            Seed = seed;
        }

        public void Initialize()
        {
            Grid = new Grid();
            Objects = new MapObjects();

            DebugStopwatch.Time("MergeLayers", () => Grid.MergeLayers() );
            DebugStopwatch.Time("NormalizeWater", () => Grid.NormalizeWater() );
            DebugStopwatch.Time("NormalizeHeight", () => Grid.NormalizeHeight() );
            DebugStopwatch.Time("MergeLayers", () => Grid.MergeLayers() );

            Colors = new Colors((int) Grid.ApplyHeightCurve(Grid.MinHeight), (int) Grid.ApplyHeightCurve(Grid.MaxHeight));

            DebugStopwatch.Time("CreateTiles", CreateTiles );
            DebugStopwatch.Time("InitTiles", InitTiles );

            DebugStopwatch.Time("CreateCities", () => Objects.CreateCities() );

            Grid = null;
        }

        internal bool WithinTileBounds(int x, int y)
        {
            return !(x < 0 || y < 0 || x >= Width || y >= Height);
        }

        internal Tile GetTile(int x, int y)
        {
            return !WithinTileBounds(x, y) ? new Tile(this, x, y, -100, -100, -100, -100) : Tiles[x, y];
        }

        internal void CreateTiles()
        {
            Tiles = new Tile[Width, Height];
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    Tiles[i, j] = new Tile(this, i, j, Grid.GetHeight(i, j), Grid.GetHeight(i + 1, j), Grid.GetHeight(i + 1, j + 1), Grid.GetHeight(i, j + 1));
                }
            }
        }

        internal void InitTiles()
        {
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    Tiles[i, j].ComputeMesh();
                }
            }
        }
    }
}
