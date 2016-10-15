using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Map
{
    public class Grid
    {
        private readonly List<Layer> _layers;
        private Dictionary<int, int> _histogram;

        public float[,] HeightMap;

        public float HorizontalScale;
        public float GlobalScale;
        public int HeightOffset;
        public float MaxHeight;
        public float MinHeight;

        public Grid()
        {
            _layers = new List<Layer>();var size = 1;

            HorizontalScale = (float) Math.Sqrt((float) GameControl.Map.Width * GameControl.Map.Height);

            var scale = 4000f;
            while (size <= 2.5 * HorizontalScale)
            {
                _layers.Add(new Layer(GameControl.Map.Seed, size, scale));
                Debug.Log("Adding layer: Size: " + size + " Scale: " + scale);
                size *= 4;
                scale = 1.5f * (float) Math.Sqrt(scale);
            }
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
            return ApplyHeightCurve((HeightMap[x, y] + HeightOffset) / GlobalScale);
        }

        internal void MergeLayers()
        {
            var width = GameControl.Map.Width;
            var height = GameControl.Map.Height;

            HeightMap = new float[GameControl.Map.Width + 1, GameControl.Map.Height + 1];
            _histogram = new Dictionary<int, int>();
            for (var i = 0; i < width + 1; i++)
            {
                for (var j = 0; j < height + 1; j++)
                {
                    var h = _layers.Sum(layer => layer.Get(i, j) / layer.Scale);

                    HeightMap[i, j] = h;
                    var roundedheight = (int) Math.Round(h);
                    int newCount;
                    _histogram.TryGetValue(roundedheight, out newCount);
                    _histogram[roundedheight] = newCount + 1;
                }
            }

            MinHeight = (_histogram.Keys.Min() + HeightOffset) / GlobalScale;
            MaxHeight = (_histogram.Keys.Max() + HeightOffset) / GlobalScale;
        }

        internal void NormalizeWater()
        {
            var count = GameControl.Map.Width * GameControl.Map.Height;
            var targetCount = count * (1 - GameControl.Map.LandPercentage / 100);
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

            var optimalMaxHeight = 17.7f * (float) Math.Pow(HorizontalScale, 0.25);
            var optimalMeanHeight = Math.Min(1.4f * (float) Math.Pow(GameControl.Map.LandPercentage, 0.75), optimalMaxHeight/2);

            var maxHeightScale = maxHeight / optimalMaxHeight;
            var meanHeightScale = meanHeight / optimalMeanHeight;

            GlobalScale = (float) Math.Sqrt(meanHeightScale * maxHeightScale);
            Debug.Log("Mean Height: " + meanHeight + ", Normalized: " + ApplyHeightCurve(meanHeight / GlobalScale));
            Debug.Log("Max Height: " + maxHeight + ", Normalized: " + ApplyHeightCurve((maxHeight + HeightOffset) / GlobalScale));
            Debug.Log("Optimal max height: " + optimalMaxHeight + ", Optimal mean height: " + optimalMeanHeight);
            Debug.Log("Global Scale: " + GlobalScale);
        }

        private void SaveHeightMaps()
        {
            var width = GameControl.Map.Width;
            var height = GameControl.Map.Height;

            var max = _histogram.Keys.Max();
            var min = _histogram.Keys.Min();
            var texture = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
            var textureColor = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
            var textureFull = new Texture2D(width + 1, height + 1, TextureFormat.RGB24, false);
            for (var i = 0; i <= width; i++)
            {
                for (var j = 0; j <= height; j++)
                {
                    var hh = GetHeight(i, j);
                    var color = Math.Max(0, hh / max);
                    var colorFull = Math.Max(0, (hh - min) / (max - min));
                    var colorIndex = GameControl.Map.Colors.GetColorIndex(hh);
                    texture.SetPixel(i, j, new Color(color, color, color));
                    textureColor.SetPixel(i, j, GameControl.Map.Colors.BorderedColors[colorIndex]);
                    textureFull.SetPixel(i, j, new Color(colorFull, colorFull, colorFull));
                }
            }
            texture.Apply();
            textureColor.Apply();
            textureFull.Apply();
            var bytes = texture.EncodeToPNG();
            var bytesColor = textureColor.EncodeToPNG();
            var bytesFull = textureFull.EncodeToPNG();
            Object.Destroy(texture);
            Object.Destroy(textureColor);
            Object.Destroy(textureFull);
            var heightMapName = GameControl.Map.Seed + "-" + width + "-" + height;
            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName + ".png", bytes);
            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName +"-Color.png", bytesColor);
            File.WriteAllBytes(Application.dataPath + "/../HeightMaps/" + heightMapName + "-Full.png", bytesFull);
        }
    }
}