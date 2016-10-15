using System;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class DebugStopwatch
    {
        public static System.Diagnostics.Stopwatch Stopwatch;

        public static System.Diagnostics.Stopwatch GetStopwatch()
        {
            return Stopwatch ?? (Stopwatch = new System.Diagnostics.Stopwatch());
        }

        public static void Time(string description, Action method)
        {
            var stopwatch = GetStopwatch();
            stopwatch.Start();
            method();
            stopwatch.Stop();
            Debug.Log(description + ": " + stopwatch.ElapsedMilliseconds + " ms");
            stopwatch.Reset();
        }
    }
}
