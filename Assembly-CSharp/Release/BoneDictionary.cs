using System;
using System.Collections.Generic;
using UnityEngine;

public class BoneDictionary
{
	public Transform transform;

	public Transform[] transforms;

	public string[] names;

	private Dictionary<string, Transform> nameDict = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

	private Dictionary<int, Transform> hashDict = new Dictionary<int, Transform>();

	public int Count => transforms.Length;

	public BoneDictionary(Transform rootBone)
	{
		this.transform = rootBone;
		transforms = rootBone.GetComponentsInChildren<Transform>(true);
		names = new string[transforms.Length];
		for (int i = 0; i < transforms.Length; i++)
		{
			Transform transform = transforms[i];
			if (transform != null)
			{
				names[i] = transform.name;
			}
		}
		BuildBoneDictionary();
	}

	public BoneDictionary(Transform rootBone, Transform[] boneTransforms, string[] boneNames)
	{
		transform = rootBone;
		transforms = boneTransforms;
		names = boneNames;
		BuildBoneDictionary();
	}

	private void BuildBoneDictionary()
	{
		for (int i = 0; i < transforms.Length; i++)
		{
			Transform value = transforms[i];
			string text = names[i];
			int hashCode = text.GetHashCode();
			if (!nameDict.ContainsKey(text))
			{
				nameDict.Add(text, value);
			}
			if (!hashDict.ContainsKey(hashCode))
			{
				hashDict.Add(hashCode, value);
			}
		}
	}

	public Transform FindBone(string name, bool defaultToRoot = true)
	{
		Transform value = null;
		if (nameDict.TryGetValue(name, out value))
		{
			return value;
		}
		if (!defaultToRoot)
		{
			return null;
		}
		return transform;
	}

	public Transform FindBone(int hash, bool defaultToRoot = true)
	{
		Transform value = null;
		if (hashDict.TryGetValue(hash, out value))
		{
			return value;
		}
		if (!defaultToRoot)
		{
			return null;
		}
		return transform;
	}
}
