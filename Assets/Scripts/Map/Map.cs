using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Map.Objects;
using Assets.Scripts.Map.Utils;
using Assets.Scripts.Utils;
using UnityEngine;
using YamlDotNet.Serialization;

namespace Assets.Scripts.Map
{
    public class Map
    {
        public Colors Colors;
        public Grid Grid;
        public Tile[,] Tiles;

        public readonly int Width;
        public readonly int Height;
        public readonly float LandPercentage;
        public readonly string Seed;

        public List<Point2<int>> Cities;
        public List<Point2<int>> Towns;

        public Map(int width , int height, int landPercentage, string seed)
        {
            Width = width;
            Height = height;
            LandPercentage = landPercentage;
            Seed = seed;

            Cities = new List<Point2<int>>();
            Towns = new List<Point2<int>>();
        }

        public void Initialize()
        {
            Grid = new Grid();

            DebugStopwatch.Time("MergeLayers", () => Grid.MergeLayers() );
            DebugStopwatch.Time("NormalizeWater", () => Grid.NormalizeWater() );
            DebugStopwatch.Time("NormalizeHeight", () => Grid.NormalizeHeight() );
            DebugStopwatch.Time("MergeLayers", () => Grid.MergeLayers() );

            Colors = new Colors((int) Grid.ApplyHeightCurve(Grid.MinHeight), (int) Grid.ApplyHeightCurve(Grid.MaxHeight));

            DebugStopwatch.Time("CreateTiles", CreateTiles );
            DebugStopwatch.Time("LocationIDConvolution", LocationIDConvolution );
            DebugStopwatch.Time("InitTiles", InitTiles );

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

        private void Convolution2D(int kernelSize, float init, Func<Tile, bool> setup, Func<Tile, Tile, float, float> iterator,
            Func<Tile, float, int, bool> finish)
        {
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var tile = GetTile(i, j);
                    var memo = init;
                    if(!setup(tile)) continue;
                    for (var ii = i - kernelSize; ii < i + kernelSize; ii++)
                    {
                        for (var jj = j - kernelSize; jj < j + kernelSize; jj++)
                        {
                            if (!WithinTileBounds(ii, jj)) continue;
                            var t = GetTile(ii, jj);
                            memo = iterator(tile, t, memo);
                        }
                    }
                    finish(tile, memo, (int) Math.Pow(kernelSize * 2 + 1, 2));
                }
            }
        }

        private void LocationIDConvolution()
        {
            var saveFileName = Application.persistentDataPath + "/mapObjects-" + Seed + "-" + Width + "-" + Height + "-" +
                               LandPercentage + "-" + MapObjects.Version + ".yml";

            if (File.Exists(saveFileName))
            {
                Debug.Log("Loading: " + saveFileName);
                var reader = new StreamReader(saveFileName);
                var deserializer = new Deserializer();
                var mapObjects = deserializer.Deserialize<MapObjects>(reader);
                Cities = mapObjects.Cities;
                Towns = mapObjects.Towns;
                return;
            }

            var globalMax = -9999f;

            Convolution2D(10, 0, tile => true, (tile, tile2, memo) =>
            {
                memo -= Math.Abs(tile2.AverageHeight() - tile.AverageHeight());
                if (tile2.AverageHeight() < 0) memo += 1.5f;
                memo -= Math.Abs(tile2.AverageHeight() - 5) / 10;

                return memo;
            }, (tile, memo, count) =>
            {
                if (tile.AverageHeight() < 0) memo -= 10000;

                tile.Color = memo / count * 10;
                globalMax = Math.Max(globalMax, tile.Color);
                return true;
            });

            Convolution2D(40, 0, tile =>
            {
                tile.Color2 = 0;
                tile.Color3 = 0;
                return tile.Color > globalMax - 5;
            }, (tile, tile2, memo) =>
            {
                if (tile2.Color > globalMax - 5 && tile != tile2)
                    memo += (float) (1f / (Math.Pow(tile2.X - tile.X, 2) + Math.Pow(tile2.Y - tile.Y, 2)));
                return memo;
            }, (tile, memo, count) =>
            {
                tile.Color2 = memo / count * 10000;
                if (tile.Color2 < 25) tile.Color2 = 0;
                tile.Color3 = tile.Color2;
                return true;
            });

            Convolution2D(40, 0,
                tile => tile.Color2 > 0,
                (tile, tile2, memo) => Math.Max(memo, tile2.Color2),
                (tile, memo, count) =>
            {
                if (Math.Abs(memo - tile.Color2) < Mathf.Epsilon)
                {
                    Cities.Add(new Point2<int> {X = tile.X, Y = tile.Y});
                }
                return true;
            });

            Convolution2D(10, 0, tile =>
            {
                tile.Color2 = 0;
                return tile.Color > globalMax - 40 && (int) tile.Color3 == 0;
            }, (tile, tile2, memo) =>
            {
                if (tile2.Dummy()) return memo;
                if (tile2.Color > globalMax - 40 && tile != tile2 && (int) tile.Color3 == 0)
                    memo += (float) (1f / (Math.Pow(tile2.X - tile.X, 2) + Math.Pow(tile2.Y - tile.Y, 2)));
                return memo;
            }, (tile, memo, count) =>
            {
                tile.Color2 = memo / count * 1500 + tile.Color * 2;
                return true;
            });

            Convolution2D(20, 0, tile => tile.Color2 > 20, (tile, tile2, memo) =>
            {
                if (tile2.Dummy()) return memo;
                memo = Math.Max(memo, tile2.Color2);
                return memo;
            }, (tile, memo, count) =>
            {
                if (Math.Abs(memo - tile.Color2) < Mathf.Epsilon)
                {
                    Towns.Add(new Point2<int> {X = tile.X, Y = tile.Y});
                }
                return true;
            });

            var mapData = new MapObjects
            {
                Cities = Cities,
                Towns = Towns
            };

            var serializer = new Serializer();
            Debug.Log(Application.persistentDataPath);
            var writer = new StreamWriter(saveFileName);
            serializer.Serialize(writer, mapData);
            writer.Close();
        }
    }
}
