using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;

namespace Assets.Scripts.Paths
{
    public class Paths : MonoBehaviour {

        private GameObject _straightPrefab;
        private GameObject _curve902Prefab;

        public Queue<PathingJob> Jobs;
        public PathingJob CurrentJob;
        public PathAutoComplete AutoComplete;

        private void Awake()
        {
            Jobs = new Queue<PathingJob>();
            AutoComplete = new PathAutoComplete();

            _straightPrefab = (GameObject) Resources.Load("prefabs/Paths/PathStraight", typeof(GameObject));
            _curve902Prefab = (GameObject) Resources.Load("prefabs/Paths/Path-90-2", typeof(GameObject));
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

        public GameObject InstantiateType(int type)
        {
            switch (type)
            {
                case 0:
                    return Instantiate(_straightPrefab);
                case 1:
                    return Instantiate(_curve902Prefab);
                case 2:
                    var part = Instantiate(_curve902Prefab);
                    part.transform.localScale = Vector3.Scale(part.transform.localScale, new Vector3(1, 1, -1));
                    return part;
                case 3:
                    return Instantiate(_curve902Prefab);
                default:
                    var part2 = Instantiate(_curve902Prefab);
                    part2.transform.localScale = Vector3.Scale(part2.transform.localScale, new Vector3(1, 1, -1));
                    return part2;
            }
        }
    }
}
