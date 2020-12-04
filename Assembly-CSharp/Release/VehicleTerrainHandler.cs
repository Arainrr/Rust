using UnityEngine;

public class VehicleTerrainHandler
{
	public enum GroundQuality
	{
		Road = 0,
		Mild = 30,
		Rough = 60,
		VeryRough = 100
	}

	public string CurGroundPhysicsMatName;

	public GroundQuality CurrentGroundQuality = GroundQuality.Mild;

	public bool IsOnIce;

	private readonly string[] TerrainRoad = new string[5]
	{
		"rock",
		"concrete",
		"gravel",
		"metal",
		"path"
	};

	private readonly string[] TerrainMild = new string[2]
	{
		"generic",
		"stones"
	};

	private readonly string[] TerrainRough = new string[4]
	{
		"dirt",
		"grass",
		"sand",
		"tundra"
	};

	private const float SECONDS_BETWEEN_TERRAIN_SAMPLE = 0.25f;

	private TimeSince timeSinceTerrainCheck;

	private readonly BaseVehicle vehicle;

	public VehicleTerrainHandler(BaseVehicle vehicle)
	{
		this.vehicle = vehicle;
	}

	public void FixedUpdate()
	{
		if (!vehicle.IsStationary() && (float)timeSinceTerrainCheck > 0.25f)
		{
			DoTerrainCheck();
		}
	}

	private void DoTerrainCheck()
	{
		timeSinceTerrainCheck = Random.Range(-0.025f, 0.025f);
		Transform transform = vehicle.transform;
		RaycastHit hitInfo;
		if (Physics.Raycast(transform.position + transform.up * 0.5f, -transform.up, out hitInfo, 1.5f, 27328512, QueryTriggerInteraction.Ignore))
		{
			CurGroundPhysicsMatName = AssetNameCache.GetNameLower(ColliderEx.GetMaterialAt(hitInfo.collider, hitInfo.point));
			IsOnIce = (CurGroundPhysicsMatName == "snow" && hitInfo.collider.name.ToLower().Contains("ice"));
		}
		else
		{
			CurGroundPhysicsMatName = "concrete";
			IsOnIce = false;
		}
		CurrentGroundQuality = GetGroundQuality(CurGroundPhysicsMatName);
	}

	private GroundQuality GetGroundQuality(string physicMat)
	{
		for (int i = 0; i < TerrainRoad.Length; i++)
		{
			if (TerrainRoad[i] == physicMat)
			{
				return GroundQuality.Road;
			}
		}
		for (int j = 0; j < TerrainMild.Length; j++)
		{
			if (TerrainRoad[j] == physicMat)
			{
				return GroundQuality.Mild;
			}
		}
		for (int k = 0; k < TerrainRough.Length; k++)
		{
			if (TerrainRoad[k] == physicMat)
			{
				return GroundQuality.Rough;
			}
		}
		return GroundQuality.VeryRough;
	}
}
