using System;
using UnityEngine;

public class Buoyancy : ListComponent<Buoyancy>
{
	private struct BuoyancyPointData
	{
		public Transform transform;

		public Vector3 localPosition;

		public Vector3 rootToPoint;

		public Vector3 position;
	}

	public BuoyancyPoint[] points;

	public GameObjectRef[] waterImpacts;

	public Rigidbody rigidBody;

	public float buoyancyScale = 1f;

	public float submergedFraction;

	public bool doEffects = true;

	public bool clientSide;

	public Action<bool> SubmergedChanged;

	public float flowMovementScale = 1f;

	private float timeInWater;

	public float requiredSubmergedFraction;

	public BaseEntity forEntity;

	private BuoyancyPointData[] pointData;

	private Vector2[] pointPositionArray;

	private Vector2[] pointPositionUVArray;

	private Vector3[] pointShoreVectorArray;

	private float[] pointTerrainHeightArray;

	private float[] pointWaterHeightArray;

	public float waveHeightScale = 0.5f;

	public float timeOutOfWater
	{
		get;
		private set;
	}

	public static string DefaultWaterImpact()
	{
		return "assets/bundled/prefabs/fx/impacts/physics/water-enter-exit.prefab";
	}

	public void Sleep()
	{
		base.enabled = false;
		InvokeRandomized(CheckForWake, 0.5f, 0.5f, 0.1f);
	}

	public void Wake()
	{
		base.enabled = true;
		CancelInvoke(CheckForWake);
		CancelInvoke(Sleep);
	}

	public void CheckForWake()
	{
		Vector3 position = base.transform.position;
		WaterLevel.WaterInfo waterInfo = WaterLevel.GetWaterInfo(position, true, forEntity);
		bool num = waterInfo.surfaceLevel > (position - Vector3.up * 0.2f).y || waterInfo.currentDepth > 0.1f;
		bool flag = !rigidBody.IsSleeping();
		if (num & flag)
		{
			Wake();
		}
	}

	protected void DoCycle()
	{
		bool flag = submergedFraction > 0f;
		BuoyancyFixedUpdate();
		bool flag2 = submergedFraction > 0f;
		if (SubmergedChanged != null && flag != flag2)
		{
			SubmergedChanged(flag2);
		}
	}

	public static void Cycle()
	{
		Buoyancy[] buffer = ListComponent<Buoyancy>.InstanceList.Values.Buffer;
		int count = ListComponent<Buoyancy>.InstanceList.Count;
		for (int i = 0; i < count; i++)
		{
			buffer[i].DoCycle();
		}
	}

	public Vector3 GetFlowDirection(Vector2 posUV)
	{
		if (TerrainMeta.WaterMap == null)
		{
			return Vector3.zero;
		}
		Vector3 normalFast = TerrainMeta.WaterMap.GetNormalFast(posUV);
		float scale = Mathf.Clamp01(Mathf.Abs(normalFast.y));
		normalFast.y = 0f;
		normalFast.FastRenormalize(scale);
		return normalFast;
	}

	public void EnsurePointsInitialized()
	{
		if (points == null || points.Length == 0)
		{
			Rigidbody component = GetComponent<Rigidbody>();
			if (component != null)
			{
				GameObject gameObject = new GameObject("BuoyancyPoint");
				gameObject.transform.parent = component.gameObject.transform;
				gameObject.transform.localPosition = component.centerOfMass;
				BuoyancyPoint buoyancyPoint = gameObject.AddComponent<BuoyancyPoint>();
				buoyancyPoint.buoyancyForce = component.mass * (0f - Physics.gravity.y);
				buoyancyPoint.buoyancyForce *= 1.32f;
				buoyancyPoint.size = 0.2f;
				points = new BuoyancyPoint[1];
				points[0] = buoyancyPoint;
			}
		}
		if (pointData == null || pointData.Length != points.Length)
		{
			pointData = new BuoyancyPointData[points.Length];
			pointPositionArray = new Vector2[points.Length];
			pointPositionUVArray = new Vector2[points.Length];
			pointShoreVectorArray = new Vector3[points.Length];
			pointTerrainHeightArray = new float[points.Length];
			pointWaterHeightArray = new float[points.Length];
			for (int i = 0; i < points.Length; i++)
			{
				Transform transform = points[i].transform;
				Transform parent = transform.parent;
				transform.SetParent(base.transform);
				Vector3 localPosition = transform.localPosition;
				transform.SetParent(parent);
				pointData[i].transform = transform;
				pointData[i].localPosition = transform.localPosition;
				pointData[i].rootToPoint = localPosition;
			}
		}
	}

	public void BuoyancyFixedUpdate()
	{
		if (TerrainMeta.WaterMap == null)
		{
			return;
		}
		EnsurePointsInitialized();
		if (rigidBody == null)
		{
			return;
		}
		if (rigidBody.IsSleeping())
		{
			Invoke(Sleep, 0f);
			return;
		}
		if (buoyancyScale == 0f)
		{
			Invoke(Sleep, 0f);
			return;
		}
		float time = Time.time;
		float x = TerrainMeta.Position.x;
		float z = TerrainMeta.Position.z;
		float x2 = TerrainMeta.OneOverSize.x;
		float z2 = TerrainMeta.OneOverSize.z;
		Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
		for (int i = 0; i < pointData.Length; i++)
		{
			BuoyancyPoint buoyancyPoint2 = points[i];
			Vector3 position = localToWorldMatrix.MultiplyPoint3x4(pointData[i].rootToPoint);
			pointData[i].position = position;
			float x3 = (position.x - x) * x2;
			float y = (position.z - z) * z2;
			pointPositionArray[i] = new Vector2(position.x, position.z);
			pointPositionUVArray[i] = new Vector2(x3, y);
		}
		WaterSystem.GetHeightArray(pointPositionArray, pointPositionUVArray, pointShoreVectorArray, pointTerrainHeightArray, pointWaterHeightArray);
		int num = 0;
		for (int j = 0; j < points.Length; j++)
		{
			BuoyancyPoint buoyancyPoint = points[j];
			Vector3 position2 = pointData[j].position;
			Vector3 localPosition = pointData[j].localPosition;
			Vector2 posUV = pointPositionUVArray[j];
			float terrainHeight = pointTerrainHeightArray[j];
			float waterHeight = pointWaterHeightArray[j];
			WaterLevel.WaterInfo buoyancyWaterInfo = WaterLevel.GetBuoyancyWaterInfo(position2, posUV, terrainHeight, waterHeight, forEntity);
			bool flag = false;
			if (position2.y < buoyancyWaterInfo.surfaceLevel)
			{
				flag = true;
				num++;
				float currentDepth = buoyancyWaterInfo.currentDepth;
				float num2 = Mathf.InverseLerp(0f, buoyancyPoint.size, currentDepth);
				float num3 = 1f + Mathf.PerlinNoise(buoyancyPoint.randomOffset + time * buoyancyPoint.waveFrequency, 0f) * buoyancyPoint.waveScale;
				float num4 = buoyancyPoint.buoyancyForce * buoyancyScale;
				Vector3 force = new Vector3(0f, num3 * num2 * num4, 0f);
				Vector3 flowDirection = GetFlowDirection(posUV);
				if (flowDirection.y < 0.9999f && flowDirection != Vector3.up)
				{
					num4 *= 0.25f;
					force.x += flowDirection.x * num4 * flowMovementScale;
					force.y += flowDirection.y * num4 * flowMovementScale;
					force.z += flowDirection.z * num4 * flowMovementScale;
				}
				rigidBody.AddForceAtPosition(force, position2, ForceMode.Force);
			}
			if (buoyancyPoint.doSplashEffects && ((!buoyancyPoint.wasSubmergedLastFrame && flag) || (!flag && buoyancyPoint.wasSubmergedLastFrame)) && doEffects && rigidBody.GetRelativePointVelocity(localPosition).magnitude > 1f)
			{
				string strName = (waterImpacts != null && waterImpacts.Length != 0 && waterImpacts[0].isValid) ? waterImpacts[0].resourcePath : DefaultWaterImpact();
				Vector3 b = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0f, UnityEngine.Random.Range(-0.25f, 0.25f));
				if (clientSide)
				{
					Effect.client.Run(strName, position2 + b, Vector3.up);
				}
				else
				{
					Effect.server.Run(strName, position2 + b, Vector3.up);
				}
				buoyancyPoint.nexSplashTime = Time.time + 0.25f;
			}
			buoyancyPoint.wasSubmergedLastFrame = flag;
		}
		if (points.Length != 0)
		{
			submergedFraction = (float)num / (float)points.Length;
		}
		if (submergedFraction > requiredSubmergedFraction)
		{
			timeInWater += Time.fixedDeltaTime;
			timeOutOfWater = 0f;
		}
		else
		{
			timeOutOfWater += Time.fixedDeltaTime;
			timeInWater = 0f;
		}
		if (timeOutOfWater > 3f && base.enabled)
		{
			Invoke(Sleep, 0f);
		}
	}
}
