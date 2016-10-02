using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Terrain
{
    public class Map
    {
        private readonly List<Layer> _layers;
        private Dictionary<int, int> _histogram;

        public Colors Colors;

        public Tile[,] Tiles;
        private float[,] _grid;

        public readonly int Width;
        public readonly int Height;

        private readonly float _landPercentage;

        public float GlobalScale;
        public int HeightOffset;

        public Map(int width , int height, int landPercentage, string seed)
        {
            Width = width;
            Height = height;

            _landPercentage = landPercentage;

            _layers = new List<Layer>();

            var size = 1;
            var scale = 4000f;
            while (size <= 2.5 * HorizontalScale())
            {
                _layers.Add(new Layer(seed, size, scale));
                Debug.Log("Adding layer: Size: " + size + " Scale: " + scale);
                size *= 4;
                scale = 1.5f * (float) Math.Sqrt(scale);
            }

            MergeLayers();

            NormalizeWater();
            NormalizeHeight();

            var minHeight = (_histogram.Keys.Min() + HeightOffset) / GlobalScale;
            var maxHeight = (_histogram.Keys.Max() + HeightOffset) / GlobalScale;

            Colors = new Colors((int) ApplyHeightCurve(minHeight), (int) ApplyHeightCurve(maxHeight));

            #region Save heightmap to PNG

//            var max = _histogram.Keys.Max();
//            var min = _histogram.Keys.Min();
//            var texture = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
//            var textureColor = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
//            var textureFull = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
//            for (var i = 0; i <= Width; i++)
//            {
//                for (var j = 0; j <= Height; j++)
//                {
//                    var hh = GetHeight(i, j);
//                    var color = Math.Max(0, hh / max);
//                    var colorFull = Math.Max(0, (hh - min) / (max - min));
//                    var colorIndex = Colors.GetColorIndex(hh);
//                    texture.SetPixel(i, j, new Color(color, color, color));
//                    textureColor.SetPixel(i, j, Colors.BorderedColors[colorIndex]);
//                    textureFull.SetPixel(i, j, new Color(colorFull, colorFull, colorFull));
//                }
//            }
//            texture.Apply();
//            textureColor.Apply();
//            textureFull.Apply();
//            var bytes = texture.EncodeToPNG();
//            var bytesColor = textureColor.EncodeToPNG();
//            var bytesFull = textureFull.EncodeToPNG();
//            Object.Destroy(texture);
//            Object.Destroy(textureColor);
//            Object.Destroy(textureFull);
//            var heightMapName = seed + "-" + Width + "-" + Height;
//            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName + ".png", bytes);
//            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName +"-Color.png", bytesColor);
//            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName + "-Full.png", bytesFull);

            #endregion

            CreateTiles();

            #region Random tile height - debug stile side rendering

            var tile = Tiles[new System.Random().Next(Width - 1), new System.Random().Next(Height - 1)];
            var h = new System.Random().Next(1, 20);
            tile.NW.y = h;
            tile.NE.y = h;
            tile.SW.y = h;
            tile.SE.y = h;

            #endregion

            InitTiles();

            _grid = null;
        }

        internal float ApplyHeightCurve(float height)
        {
            float newHeight;
            if (height > -1 && height <= 1) newHeight = (float) Math.Sqrt(height + 1) - 1;
            else if (height > 1 && height <= 52) newHeight = (float) Math.Pow(height * 0.75 / 65 + 0.25, 3) * 70 - 0.35f;
            else if (height > 52) newHeight = (float) Math.Sqrt(height - 45) * 10 + 16.2f;
            else newHeight = height;

            return (float) Math.Round(newHeight, 1);
        }

        internal float GetHeight(int x, int y)
        {
            return ApplyHeightCurve((_grid[x, y] + HeightOffset) / GlobalScale);
        }

        internal Tile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Width) return new Tile(this, x, y, -100, -100, -100, -100);
            return Tiles[x, y];
        }

        internal void MergeLayers()
        {
            _grid = new float[Width + 1, Height + 1];
            _histogram = new Dictionary<int, int>();
            for (var i = 0; i < Width + 1; i++)
            {
                for (var j = 0; j < Height + 1; j++)
                {
                    var height = _layers.Sum(layer => layer.Get(i, j) / layer.Scale);

                    _grid[i, j] = height;
                    var roundedHeight = (int) Math.Round(height);
                    int newCount;
                    _histogram.TryGetValue(roundedHeight, out newCount);
                    _histogram[roundedHeight] = newCount + 1;
                }
            }
        }

        internal float HorizontalScale()
        {
            return (float) Math.Sqrt((float) Width * Height);
        }

        internal void NormalizeWater()
        {
            var count = Width * Height;
            var targetCount = count * (1 - _landPercentage / 100);
            var keys = new List<int>(_histogram.Keys);
            keys.Sort();
            var keyIndex = 0;
            var sum = 0;
            while (sum < targetCount)
            {
                int value;
                _histogram.TryGetValue(keys[keyIndex], out value);
                sum += value;
                keyIndex++;
            }
            HeightOffset = -keys[keyIndex];
            Debug.Log("Height Offset: " + HeightOffset);

        }

        internal void NormalizeHeight()
        {
            var keys = new List<int>(_histogram.Keys);
            var aboveSeaCount = 0;
            var sum = 0;
            foreach (var key in keys)
            {
                if (key + HeightOffset < 0) continue;
                var count = _histogram[key];
                sum += count * (key + HeightOffset);
                aboveSeaCount += count;
            }

            var meanHeight = (float) sum / aboveSeaCount;
            var maxHeight = _histogram.Keys.Max();

            var optimalMaxHeight = 17.7f * (float) Math.Pow(HorizontalScale(), 0.25);
            var optimalMeanHeight = Math.Min(1.4f * (float) Math.Pow(_landPercentage, 0.75), optimalMaxHeight/2);

            var maxHeightScale = maxHeight / optimalMaxHeight;
            var meanHeightScale = meanHeight / optimalMeanHeight;

            GlobalScale = (float) Math.Sqrt(meanHeightScale * maxHeightScale);
            Debug.Log("Mean Height: " + meanHeight + ", Normalized: " + ApplyHeightCurve(meanHeight / GlobalScale));
            Debug.Log("Max Height: " + maxHeight + ", Normalized: " + ApplyHeightCurve((maxHeight + HeightOffset) / GlobalScale));
            Debug.Log("Optimal max height: " + optimalMaxHeight + ", Optimal mean height: " + optimalMeanHeight);
            Debug.Log("Global Scale: " + GlobalScale);
        }

        internal void CreateTiles()
        {
            Tiles = new Tile[Width, Height];
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    Tiles[i, j] = new Tile(this, i, j, GetHeight(i, j), GetHeight(i + 1, j), GetHeight(i + 1, j + 1), GetHeight(i, j + 1));
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
