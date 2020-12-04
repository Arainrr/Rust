using System.Collections.Generic;
using UnityEngine;

public class TerrainPath : TerrainExtension
{
	public List<PathList> Roads = new List<PathList>();

	public List<PathList> Rivers = new List<PathList>();

	public List<PathList> Powerlines = new List<PathList>();

	public List<MonumentInfo> Monuments = new List<MonumentInfo>();

	public List<RiverInfo> RiverObjs = new List<RiverInfo>();

	public List<LakeInfo> LakeObjs = new List<LakeInfo>();

	public List<Vector3> OceanPatrolClose = new List<Vector3>();

	public List<Vector3> OceanPatrolFar = new List<Vector3>();

	public Dictionary<string, List<PowerlineNode>> wires = new Dictionary<string, List<PowerlineNode>>();

	public override void PostSetup()
	{
		foreach (PathList road in Roads)
		{
			road.ProcgenStartNode = null;
			road.ProcgenEndNode = null;
		}
		foreach (PathList river in Rivers)
		{
			river.ProcgenStartNode = null;
			river.ProcgenEndNode = null;
		}
		foreach (PathList powerline in Powerlines)
		{
			powerline.ProcgenStartNode = null;
			powerline.ProcgenEndNode = null;
		}
	}

	public void Clear()
	{
		Roads.Clear();
		Rivers.Clear();
		Powerlines.Clear();
	}

	public static int[,] CreatePowerlineCostmap(ref uint seed)
	{
		float radius = 5f;
		int num = (int)((float)(double)World.Size / 7.5f);
		TerrainHeightMap heightMap = TerrainMeta.HeightMap;
		TerrainTopologyMap topologyMap = TerrainMeta.TopologyMap;
		int[,] array = new int[num, num];
		for (int i = 0; i < num; i++)
		{
			float normZ = ((float)i + 0.5f) / (float)num;
			for (int j = 0; j < num; j++)
			{
				float normX = ((float)j + 0.5f) / (float)num;
				float slope = heightMap.GetSlope(normX, normZ);
				int topology = topologyMap.GetTopology(normX, normZ, radius);
				int num2 = 2295174;
				int num3 = 55296;
				int num4 = 512;
				if ((topology & num2) != 0)
				{
					array[i, j] = int.MaxValue;
				}
				else if ((topology & num3) != 0)
				{
					array[i, j] = 2500;
				}
				else if ((topology & num4) != 0)
				{
					array[i, j] = 1000;
				}
				else
				{
					array[i, j] = 1 + (int)(slope * slope * 10f);
				}
			}
		}
		return array;
	}

	public static int[,] CreateRoadCostmap(ref uint seed)
	{
		float radius = 5f;
		int num = (int)((float)(double)World.Size / 7.5f);
		TerrainHeightMap heightMap = TerrainMeta.HeightMap;
		TerrainTopologyMap topologyMap = TerrainMeta.TopologyMap;
		int[,] array = new int[num, num];
		for (int i = 0; i < num; i++)
		{
			float normZ = ((float)i + 0.5f) / (float)num;
			for (int j = 0; j < num; j++)
			{
				float normX = ((float)j + 0.5f) / (float)num;
				int num2 = SeedRandom.Range(ref seed, 100, 200);
				float slope = heightMap.GetSlope(normX, normZ);
				int topology = topologyMap.GetTopology(normX, normZ, radius);
				int num3 = 2295686;
				int num4 = 49152;
				if (slope > 20f || (topology & num3) != 0)
				{
					array[i, j] = int.MaxValue;
				}
				else if ((topology & num4) != 0)
				{
					array[i, j] = 2500;
				}
				else
				{
					array[i, j] = 1 + (int)(slope * slope * 10f) + num2;
				}
			}
		}
		return array;
	}

	public void AddWire(PowerlineNode node)
	{
		string name = node.transform.root.name;
		if (!wires.ContainsKey(name))
		{
			wires.Add(name, new List<PowerlineNode>());
		}
		wires[name].Add(node);
	}

	public void CreateWires()
	{
		List<GameObject> list = new List<GameObject>();
		int num = 0;
		GameObjectRef gameObjectRef = null;
		foreach (KeyValuePair<string, List<PowerlineNode>> wire in wires)
		{
			foreach (PowerlineNode item in wire.Value)
			{
				PowerLineWireConnectionHelper component = item.GetComponent<PowerLineWireConnectionHelper>();
				if ((bool)component)
				{
					if (list.Count == 0)
					{
						gameObjectRef = item.WirePrefab;
						num = component.connections.Count;
					}
					else
					{
						GameObject gameObject = list[list.Count - 1];
						if (item.WirePrefab.guid != gameObjectRef?.guid || component.connections.Count != num || (gameObject.transform.position - item.transform.position).sqrMagnitude > item.MaxDistance * item.MaxDistance)
						{
							CreateWire(wire.Key, list, gameObjectRef);
							list.Clear();
						}
					}
					list.Add(item.gameObject);
				}
			}
			CreateWire(wire.Key, list, gameObjectRef);
			list.Clear();
		}
	}

	private void CreateWire(string name, List<GameObject> objects, GameObjectRef wirePrefab)
	{
		if (objects.Count >= 3 && wirePrefab != null && wirePrefab.isValid)
		{
			PowerLineWire powerLineWire = PowerLineWire.Create(null, objects, wirePrefab, "Powerline Wires", null, 1f, 0.1f);
			if ((bool)powerLineWire)
			{
				powerLineWire.enabled = false;
				GameObjectEx.SetHierarchyGroup(powerLineWire.gameObject, name);
			}
		}
	}
}
