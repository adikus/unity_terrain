using UnityEngine;

namespace Assets.Scripts.UI
{
    public class FPSCounter
    {
        private const float FPSMeasurePeriod = 0.5f;
        private int _mFpsAccumulator;
        private float _mFpsNextPeriod;
        public int FPS;

        public FPSCounter()
        {
            _mFpsNextPeriod = Time.realtimeSinceStartup + FPSMeasurePeriod;
        }

        internal void Update()
        {
            _mFpsAccumulator++;
            if (!(Time.realtimeSinceStartup > _mFpsNextPeriod)) return;
            FPS = (int) (_mFpsAccumulator/FPSMeasurePeriod);
            _mFpsAccumulator = 0;
            _mFpsNextPeriod += FPSMeasurePeriod;
        }
    }
}