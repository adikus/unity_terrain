using System;

using UnityEngine;

using Assets.Scripts.Map.Utils;

namespace Assets.Scripts.Map.LocationID
{
    public class Identification
    {
        public void IdentifyCities()
        {
            var globalMax = -9999f;

            Convolution.Tile2D(10, 0, tile => true, (tile, tile2, memo) =>
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

            Convolution.Tile2D(40, 0, tile =>
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

            Convolution.Tile2D(40, 0,
                tile => tile.Color2 > 0,
                (tile, tile2, memo) => Math.Max(memo, tile2.Color2),
                (tile, memo, count) =>
                {
                    if (Math.Abs(memo - tile.Color2) < Mathf.Epsilon)
                    {
                        GameControl.Map.Objects.Cities.Add(new Point2<int> {X = tile.X, Y = tile.Y});
                    }
                    return true;
                });

            Convolution.Tile2D(10, 0, tile =>
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

            Convolution.Tile2D(20, 0, tile => tile.Color2 > 20, (tile, tile2, memo) =>
            {
                if (tile2.Dummy()) return memo;
                memo = Math.Max(memo, tile2.Color2);
                return memo;
            }, (tile, memo, count) =>
            {
                if (Math.Abs(memo - tile.Color2) < Mathf.Epsilon)
                {
                    GameControl.Map.Objects.Towns.Add(new Point2<int> {X = tile.X, Y = tile.Y});
                }
                return true;
            });
        }
    }
}
