using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;

namespace Assets.Scripts.Paths
{
    public class Paths : MonoBehaviour {

        public GameObject StraightPrefab;

        public Queue<PathingJob> Jobs;
        public PathingJob CurrentJob;
        public PathAutoComplete AutoComplete;

        private void Awake()
        {
            Jobs = new Queue<PathingJob>();
        }

        private void Start()
        {
            StraightPrefab = (GameObject) Resources.Load("prefabs/Paths/PathStraight", typeof(GameObject));
            AutoComplete = new PathAutoComplete();
        }

        private void Update ()
        {
            if (CurrentJob == null)
            {
                if (Jobs.Count > 0) StartNextJob();
                else return;
            }

            if (CurrentJob.Heap.Count > 0)
            {
                GameControl.UI.DebugLines[2] = "Cost: " + (int) CurrentJob.CostEstimate(CurrentJob.Heap.First().Value);
                GameControl.UI.DebugLines[2] += " Counter: " + CurrentJob.Counter;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < 1000.0 / 60)
            {
                if (CurrentJob.Done)
                {
                    CurrentJob = null;
                    break;
                }
                CurrentJob.Step();
            }

            stopwatch.Stop();
        }

        public void StartNextJob()
        {
            CurrentJob = Jobs.Dequeue();
            CurrentJob.StartJob();
        }
    }
}
