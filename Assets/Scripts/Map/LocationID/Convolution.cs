using System;

namespace Assets.Scripts.Map.LocationID
{
    public class Convolution
    {
        public static void Tile2D(int kernelSize, float init, Func<Tile, bool> setup,
            Func<Tile, Tile, float, float> iterator, Func<Tile, float, int, bool> finish)
        {
            for (var i = 0; i < GameControl.Map.Width; i++)
            {
                for (var j = 0; j < GameControl.Map.Height; j++)
                {
                    var tile = GameControl.Map.GetTile(i, j);
                    var memo = init;
                    if(!setup(tile)) continue;
                    for (var ii = i - kernelSize; ii < i + kernelSize; ii++)
                    {
                        for (var jj = j - kernelSize; jj < j + kernelSize; jj++)
                        {
                            if (!GameControl.Map.WithinTileBounds(ii, jj)) continue;
                            var t = GameControl.Map.GetTile(ii, jj);
                            memo = iterator(tile, t, memo);
                        }
                    }
                    finish(tile, memo, (int) Math.Pow(kernelSize * 2 + 1, 2));
                }
            }
        }
    }
}
