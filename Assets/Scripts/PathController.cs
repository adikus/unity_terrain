using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PathController : MonoBehaviour {

    public GameObject StraightPrefab;
    public GameObject Curve902Prefab;
    public GameObject TerrainController;

    public TerrainGeneration TerrainGeneration;

    // Use this for initialization
	void Start ()
	{
	    TerrainGeneration = TerrainController.GetComponent<TerrainGeneration>();
	}
	
	// Update is called once per frame
	void Update ()
	{
	    if (Heap == null) return;
	    //if (!Input.GetKey(KeyCode.P)) return;

	    var stopwatch = new System.Diagnostics.Stopwatch();
	    stopwatch.Start();
	    while (stopwatch.ElapsedMilliseconds < 1000.0/60)
	    {
	        if (Heap.Count == 0) return;
	        var current = Heap.First();

	        //if (CostEstimate(current.Value) < 1)
	        if (current.Value.X == Goal.X && current.Value.Y == Goal.Y)
	        {
	            Heap.Clear();
	            ShowPath(current.Value);
	            return;
	        }

	        current.Value.Hide();
	        Heap.RemoveAt(0);
	        for (var i = 0; i < 3; i++)
	        {
	            for (var z = -0.2f; z <= 0.2f; z += 0.2f)
	            {
	                var part = AddPart(current.Value, i, z);
	                if (part.IsPossible())
	                {
	                    var key = part.X + "," + part.Y + "," + part.Type;// + "," + part.Direction;
	                    if (Set.ContainsKey(key))
	                    {
	                        var existingPart = Set[key];
	                        if (existingPart.Cost > part.Cost)
	                        {
	                            Set[key] = part;
	                        }
	                        else
	                        {
	                            continue;
	                        }
	                    }
	                    else
	                    {
	                        Set.Add(key, part);
	                    }
	                    if (!Heap.ContainsKey(CostEstimate(part) + part.Cost))
	                    {
	                        part.Show();
	                        Parts.Add(part);
	                        Heap.Add(CostEstimate(part) + part.Cost, part);
	                    }
	                }
	                else
	                {
	                    part.Destroy();
	                }
	            }
	        }
	    }
	    stopwatch.Stop();
	}

    public List<PathPart> Parts;
    public Point3 Goal;
    public SortedList<float, PathPart> Heap;
    public Dictionary<string, PathPart> Set;

    public void Test()
    {
        var goalTile = TerrainGeneration.Map.GetTile(195, 195);
        Goal = new Point3 { X = goalTile.X, Y = goalTile.Y, Z = goalTile.AverageHeight() };
        Heap = new SortedList<float, PathPart>();
        Set = new Dictionary<string, PathPart>();
        Parts = new List<PathPart>();
        var startTile = TerrainGeneration.Map.GetTile(195, 195);
        var part = new PathPart(this, null, 0, 0, 0, startTile.AverageHeight(), 0);
        Parts.Add(part);
        Heap.Add(CostEstimate(part) + part.Cost, part);


        /*
        Parts.Add(new PathPart(this, null, 0, 2, 0, 180));
        var path = new [] {1, 0, 1, 2, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0};

        foreach (var type in path)
        {
            AddPart(type);
        }*/
    }

    public void ShowPath(PathPart finalPart)
    {
        foreach (var part in Parts)
        {
            part.Hide();
        }

        var current = finalPart;
        while (current != null)
        {
            current.Show();
            current = current.Previous;
        }
    }

    public float CostEstimate(PathPart part)
    {
        return (float) Math.Sqrt(Math.Pow(part.EndX - Goal.X, 2) + Math.Pow(part.EndY - Goal.Y, 2) + Math.Pow(part.Z - Goal.Z, 2));
    }

    public PathPart AddPart(int type, float z)
    {
        var lastPart = Parts.Last();
        return new PathPart(this, lastPart, type, lastPart.EndX, lastPart.EndY, lastPart.Z + z, lastPart.EndDirection);
    }

    public PathPart AddPart(PathPart previous, int type, float z)
    {
        return new PathPart(this, previous, type, previous.EndX, previous.EndY, previous.Z + z, previous.EndDirection);
    }

    public GameObject InstantiateType(int type)
    {
        switch (type)
        {
            case 0:
                return Instantiate(StraightPrefab);
            case 1:
                return Instantiate(Curve902Prefab);
            default:
                var part = Instantiate(Curve902Prefab);
                part.transform.localScale = Vector3.Scale(part.transform.localScale, new Vector3(1, 1, -1));
                return part;
        }
    }
}

public class PathPart
{
    public int Direction;
    public int X;
    public int Y;
    public float Z;
    public int Type;

    public int EndX;
    public int EndY;
    public int EndDirection;

    public float Cost;

    private PathController _pathController;
    private GameObject _object;

    public PathPart Previous;

    public PathPart(PathController pathController, PathPart previous, int type, int x, int y, float z, int direction)
    {
        Previous = previous;
        Type = type;
        X = x;
        Y = y;
        Z = z;
        Direction = direction;
        _pathController = pathController;

        var path = type == 0 ? new Vector3(1, 0, 0) : (type == 1 ? new Vector3(1, 0, -2) : new Vector3(1, 0, 2));
        path = Quaternion.AngleAxis(Direction, Vector3.up) * path;
        var end = path + new Vector3(X, 0, Y);
        EndX = (int) Math.Round(end.x);
        EndY = (int) Math.Round(end.z);
        var angle = type == 0 ? 0 : (type == 1 ? 90 : -90);
        EndDirection = Direction + angle;

        Cost = (Previous != null ? Previous.Cost : 0) + PartCost();
    }

    public bool IsPossible()
    {
        var map = _pathController.TerrainGeneration.Map;
        var endTile = map.GetTile(EndX, EndY);
        return !endTile.Dummy();
    }

    public void Destroy()
    {
        UnityEngine.Object.Destroy(_object);
    }

    public void Hide()
    {
        if (_object == null) return;
        UnityEngine.Object.Destroy(_object);
    }

    public void Show()
    {
        if (_object != null) return;
        _object = _pathController.InstantiateType(Type);

        var cubeOffset = new Vector3(0.5f, 0.5f, 0.5f);

        _object.transform.position = _pathController.TerrainGeneration.Offset + cubeOffset + new Vector3(X, Z/2, Y);
        _object.transform.Rotate(Vector3.up, Direction);

        var cost = (_pathController.CostEstimate(this) + Cost) % 50 / 50;
        var color = new Color(cost, cost, cost);

        if (_object.transform.childCount > 0)
        {
            foreach (Transform child in _object.transform)
            {
                child.GetComponent<Renderer>().material.color = color;//Type == 1 ? Color.blue : Color.green;
            }
        }
        else
        {
            _object.GetComponent<Renderer>().material.color = color;//Color.red;
        }
    }

    public float PartCost()
    {
        var map = _pathController.TerrainGeneration.Map;
        var tile = map.GetTile(X, Y);
        var endTile = map.GetTile(EndX, EndY);
        var baseCost = Type == 0 ? 1f : 2f;
        var heightDiffCost = Previous == null ? 0 : Math.Abs(Z - Previous.Z);
        var heightCost = Math.Abs(Z - tile.AverageHeight());
        heightCost += Math.Abs(Z - endTile.AverageHeight());
        var waterCost = Z < 0 ? -Z : 0;
        return baseCost + 10*heightDiffCost + 3*heightCost + waterCost;
    }
}
