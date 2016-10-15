using UnityEngine;

namespace Assets.Scripts.Map
{
    internal class Layer
    {
        public int Size;
        public float Scale;

        private readonly int _seedWidthOffset;
        private readonly int _seedHeightOffset;

        public Layer(string seed, int size, float scale)
        {

            var rnd = new System.Random((seed + size + scale).GetHashCode());
            _seedWidthOffset = rnd.Next(10000);
            _seedHeightOffset = rnd.Next(10000);

            Size = size;
            Scale = scale;
        }

        internal float Get(int x, int y)
        {
            return 100 * Mathf.PerlinNoise(_seedWidthOffset + 0.5f * x / Size, _seedHeightOffset + 0.5f * y / Size);
        }
    }
}
