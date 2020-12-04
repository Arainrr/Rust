using ConVar;
using Oxide.Core;
using Rust;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseBoat : BaseVehicle
{
	public float engineThrust = 10f;

	public float steeringScale = 0.1f;

	public float gasPedal;

	public float steering;

	public Transform thrustPoint;

	public Transform centerOfMass;

	public Buoyancy buoyancy;

	public GameObject clientCollider;

	public GameObject serverCollider;

	[ServerVar]
	public static bool generate_paths = true;

	public bool InDryDock()
	{
		return GetParentEntity() != null;
	}

	public override float MaxVelocity()
	{
		return 25f;
	}

	public override void ServerInit()
	{
		base.ServerInit();
		rigidBody.isKinematic = false;
		if (rigidBody == null)
		{
			Debug.LogWarning("Boat rigidbody null");
		}
		else if (centerOfMass == null)
		{
			Debug.LogWarning("boat COM null");
		}
		else
		{
			rigidBody.centerOfMass = centerOfMass.localPosition;
		}
	}

	public override void PlayerServerInput(InputState inputState, BasePlayer player)
	{
		if (IsDriver(player))
		{
			DriverInput(inputState, player);
		}
	}

	public virtual void DriverInput(InputState inputState, BasePlayer player)
	{
		if (inputState.IsDown(BUTTON.FORWARD))
		{
			gasPedal = 1f;
		}
		else if (inputState.IsDown(BUTTON.BACKWARD))
		{
			gasPedal = -0.5f;
		}
		else
		{
			gasPedal = 0f;
		}
		if (inputState.IsDown(BUTTON.LEFT))
		{
			steering = 1f;
		}
		else if (inputState.IsDown(BUTTON.RIGHT))
		{
			steering = -1f;
		}
		else
		{
			steering = 0f;
		}
	}

	public virtual bool EngineOn()
	{
		if (HasDriver())
		{
			return !IsFlipped();
		}
		return false;
	}

	protected override void VehicleFixedUpdate()
	{
		if (!base.isClient)
		{
			if (!EngineOn())
			{
				gasPedal = 0f;
				steering = 0f;
			}
			base.VehicleFixedUpdate();
			bool flag = WaterLevel.Test(thrustPoint.position, true, this);
			if (gasPedal != 0f && flag && buoyancy.submergedFraction > 0.3f)
			{
				Vector3 force = (base.transform.forward + base.transform.right * steering * steeringScale).normalized * gasPedal * engineThrust;
				rigidBody.AddForceAtPosition(force, thrustPoint.position, ForceMode.Force);
			}
		}
	}

	public void BaseBoatDecay(float decayTickRate, float timeSinceLastUsed, float outsideDecayMinutes, float deepWaterDecayMinutes)
	{
		if (!(timeSinceLastUsed < 2700f))
		{
			float overallWaterDepth = WaterLevel.GetOverallWaterDepth(base.transform.position);
			float num = IsOutside() ? outsideDecayMinutes : float.PositiveInfinity;
			if (overallWaterDepth > 4f)
			{
				float t = Mathf.InverseLerp(4f, 12f, overallWaterDepth);
				float num2 = Mathf.Lerp(0.1f, 1f, t);
				num = Mathf.Min(num, deepWaterDecayMinutes / num2);
			}
			if (!float.IsPositiveInfinity(num))
			{
				float num3 = decayTickRate / 60f / num;
				Hurt(MaxHealth() * num3, DamageType.Decay, this, false);
			}
		}
	}

	public virtual bool EngineInWater()
	{
		return TerrainMeta.WaterMap.GetHeight(thrustPoint.position) > thrustPoint.position.y;
	}

	public override float WaterFactorForPlayer(BasePlayer player)
	{
		if (TerrainMeta.WaterMap.GetHeight(player.eyes.position) >= player.eyes.position.y)
		{
			return 1f;
		}
		return 0f;
	}

	public static float GetWaterDepth(Vector3 pos)
	{
		if (!UnityEngine.Application.isPlaying || TerrainMeta.WaterMap == null)
		{
			RaycastHit hitInfo;
			if (!UnityEngine.Physics.Raycast(pos, Vector3.down, out hitInfo, 100f, 8388608))
			{
				return 100f;
			}
			return hitInfo.distance;
		}
		return TerrainMeta.WaterMap.GetDepth(pos);
	}

	public static List<Vector3> GenerateOceanPatrolPath(float minDistanceFromShore = 50f, float minWaterDepth = 8f)
	{
		object obj = Interface.CallHook("OnBoatPathGenerate");
		if (obj is List<Vector3>)
		{
			return (List<Vector3>)obj;
		}
		float x = TerrainMeta.Size.x;
		float num = x * 2f * (float)Math.PI;
		float num2 = 30f;
		int num3 = Mathf.CeilToInt(num / num2);
		List<Vector3> list = new List<Vector3>();
		float num4 = x;
		float y = 0f;
		for (int i = 0; i < num3; i++)
		{
			float num5 = (float)i / (float)num3 * 360f;
			list.Add(new Vector3(Mathf.Sin(num5 * ((float)Math.PI / 180f)) * num4, y, Mathf.Cos(num5 * ((float)Math.PI / 180f)) * num4));
		}
		float d = 4f;
		float num6 = 200f;
		bool flag = true;
		for (int j = 0; j < AI.ocean_patrol_path_iterations; j++)
		{
			if (!flag)
			{
				break;
			}
			flag = false;
			for (int k = 0; k < num3; k++)
			{
				Vector3 vector = list[k];
				int index = (k == 0) ? (num3 - 1) : (k - 1);
				int index2 = (k != num3 - 1) ? (k + 1) : 0;
				Vector3 b = list[index2];
				Vector3 b2 = list[index];
				Vector3 origin = vector;
				Vector3 normalized = (Vector3.zero - vector).normalized;
				Vector3 vector2 = vector + normalized * d;
				if (Vector3.Distance(vector2, b) > num6 || Vector3.Distance(vector2, b2) > num6)
				{
					continue;
				}
				bool flag2 = true;
				int num7 = 16;
				for (int l = 0; l < num7; l++)
				{
					float num8 = (float)l / (float)num7 * 360f;
					Vector3 normalized2 = new Vector3(Mathf.Sin(num8 * ((float)Math.PI / 180f)), y, Mathf.Cos(num8 * ((float)Math.PI / 180f))).normalized;
					Vector3 vector3 = vector2 + normalized2 * 1f;
					GetWaterDepth(vector3);
					Vector3 direction = normalized;
					if (vector3 != Vector3.zero)
					{
						direction = (vector3 - vector2).normalized;
					}
					RaycastHit hitInfo;
					if (UnityEngine.Physics.SphereCast(origin, 3f, direction, out hitInfo, minDistanceFromShore, 1218511105))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					flag = true;
					list[k] = vector2;
				}
			}
		}
		if (flag)
		{
			Debug.LogWarning("Failed to generate ocean patrol path");
			return null;
		}
		List<int> list2 = new List<int>();
		LineUtility.Simplify(list, 5f, list2);
		List<Vector3> list3 = list;
		list = new List<Vector3>();
		foreach (int item in list2)
		{
			list.Add(list3[item]);
		}
		Debug.Log("Generated ocean patrol path with node count: " + list.Count);
		return list;
	}
}
