﻿using UnityEngine;

namespace Terrain
{
    public class Texture
    {
        private readonly Texture2D _texture;
        private readonly int _resolution;
        private readonly Color _borderColor = new Color(0f, 0f, 0f);

        public Texture(int resolution, Colors colors)
        {
            _resolution = resolution;

            var borderedColors = colors.BorderedColors;

            var borderlessColors = colors.BorderlessColors;

            _texture = new Texture2D(resolution * (borderedColors.Count + borderlessColors.Count), resolution, TextureFormat.RGB24, true);

            for (var i = 0; i < borderedColors.Count; i++)
            {
                AddColorSquareWithBorder(borderedColors[i], i);
            }

            for (var i = 0; i < borderlessColors.Count; i++)
            {
                AddColorSquare(borderlessColors[i], i + borderedColors.Count);
            }

            _texture.Apply();
        }

        internal void AttachTextureTo(Material material)
        {
            material.mainTexture = _texture;
        }

        private void AddColorSquareWithBorder(Color color, int index)
        {
            for (var i = _resolution * index; i < _resolution * (index + 1); i++)
            {
                for (var j = 0; j < _resolution; j++)
                {
                    if (i == _resolution * index || i == _resolution * (index + 1) - 1 || j == 0 || j == _resolution - 1)
                    {
                        _texture.SetPixel(i, j, _borderColor);
                    }
                    else
                    {
                        _texture.SetPixel(i, j, color);
                    }
                }
            }
        }

        private void AddColorSquare(Color color, int index)
        {
            for (var i = _resolution * index; i < _resolution * (index + 1); i++)
            {
                for (var j = 0; j < _resolution; j++)
                {
                    _texture.SetPixel(i, j, color);
                }
            }
        }
    }
}
