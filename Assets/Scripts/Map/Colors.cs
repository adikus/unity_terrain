using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Map
{
    public class Colors
    {
        private static readonly int[] ColorStops = { -25, -10, 0, 10, 32, 42, 60, 1000 };

        private static readonly int[,] ColorMap =
        {
            {18, 29, 99}, // Deep blue
            {53, 145, 176}, // Light blue
            {214, 199, 84}, //Sand
            {22, 115, 14}, // Green
            {12, 59, 8}, // Dark green
            {115, 100, 84}, // Brown grey
            {120, 120, 120}, // Grey
            {255,255,255} // White
        };

        private static readonly int[] GradientMap =
        {
            0,
            14, //Light blue to deep blue
            5, // Sand to light blue
            4, // Green to sand
            20, // Dark green to green
            5, // Brown to green
            2, // Grey to brown
            3 // White to grey
        };

        private readonly int _minHeight;
        private readonly int _maxHeight;

        public List<Color> BorderedColors = new List<Color>();
        public List<Color> BorderlessColors = new List<Color>();

        public Colors(int minHeight, int maxHeight)
        {
            _minHeight = Math.Max(minHeight, ColorStops[0]);
            _maxHeight = Math.Min(maxHeight, ColorStops[ColorStops.Length - 2] + GradientMap[GradientMap.Length - 1]);
            Debug.Log("Generating colors from: " + _minHeight + " to: " + _maxHeight);

            var brownIndex = 5;
            BorderlessColors.Add(new Color((float) ColorMap[brownIndex, 0] / 255, (float) ColorMap[brownIndex, 1] / 255,
                (float) ColorMap[brownIndex, 2] / 255));

            for (var height = _minHeight; height <= _maxHeight; height++)
            {
                var i = 0;
                while (ColorStops[i] < height){ i++; }

                if (GradientMap[i] == 0)
                {
                    BorderedColors.Add(new Color((float) ColorMap[i, 0]/255, (float) ColorMap[i, 1] / 255, (float) ColorMap[i, 2] / 255));
                    continue;
                }
                var newColor = new [] {0, 0, 0};
                for (var j = 0; j < 3; j++)
                {
                    var v = ColorMap[i, j];
                    var c = ColorMap[i - 1, j];

                    var g = GradientMap[i];
                    var rV = Math.Min(g, height - ColorStops[i - 1]);
                    var rC = Math.Max(0, ColorStops[i - 1] + g - height);

                    newColor[j] = (v * rV + c * rC) / g;
                }

                BorderedColors.Add(new Color((float) newColor[0] / 255, (float) newColor[1] / 255, (float) newColor[2] / 255));
            }
        }

        public int GetColorIndex(float height)
        {
            return Mathf.Clamp(Math.Min(Math.Max((int) height - _minHeight, 0), _maxHeight - _minHeight), 0, BorderedColors.Count - 1);
        }

        public int GetBorderlessIndex(int index)
        {
            return index + BorderedColors.Count;
        }
    }
}
