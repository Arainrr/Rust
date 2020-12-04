using UnityEngine;

public static class TerrainModifierEx
{
	public static void ApplyTerrainModifiers(this Transform transform, TerrainModifier[] modifiers, Vector3 pos, Quaternion rot, Vector3 scale)
	{
		foreach (TerrainModifier obj in modifiers)
		{
			Vector3 point = Vector3.Scale(obj.worldPosition, scale);
			Vector3 pos2 = pos + rot * point;
			float y = scale.y;
			obj.Apply(pos2, y);
		}
	}

	public static void ApplyTerrainModifiers(this Transform transform, TerrainModifier[] modifiers)
	{
		ApplyTerrainModifiers(transform, modifiers, transform.position, transform.rotation, transform.lossyScale);
	}
}
