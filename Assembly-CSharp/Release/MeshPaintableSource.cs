using Facepunch;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshPaintableSource : MonoBehaviour, IClientComponent
{
	public int texWidth = 256;

	public int texHeight = 128;

	public string replacementTextureName = "_DecalTexture";

	public float cameraFOV = 60f;

	public float cameraDistance = 2f;

	[NonSerialized]
	public Texture2D texture;

	public GameObject sourceObject;

	public Mesh collisionMesh;

	public Vector3 localPosition;

	public Vector3 localRotation;

	private static MaterialPropertyBlock block;

	public void Init()
	{
		if (texture == null)
		{
			texture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
			texture.name = "MeshPaintableSource_" + base.gameObject.name;
			texture.Clear(Color.clear);
		}
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		else
		{
			block.Clear();
		}
		UpdateMaterials(block);
		List<Renderer> obj = Pool.GetList<Renderer>();
		base.transform.root.GetComponentsInChildren(true, obj);
		foreach (Renderer item in obj)
		{
			item.SetPropertyBlock(block);
		}
		Pool.FreeList(ref obj);
	}

	public void Free()
	{
		if ((bool)texture)
		{
			UnityEngine.Object.Destroy(texture);
			texture = null;
		}
	}

	public virtual void UpdateMaterials(MaterialPropertyBlock block, Texture2D textureOverride = null)
	{
		block.SetTexture(replacementTextureName, textureOverride ?? texture);
	}

	public void UpdateFrom(Texture2D input)
	{
		Init();
		texture.SetPixels32(input.GetPixels32());
		texture.Apply(true, false);
	}

	public void Load(byte[] data)
	{
		Init();
		if (data != null)
		{
			texture.LoadImage(data);
			if (texture.width != texWidth || texture.height != texHeight)
			{
				texture.Resize(texWidth, texHeight);
			}
			texture.Apply(true, false);
		}
	}

	public void Clear()
	{
		if (!(texture == null))
		{
			texture.Clear(new Color(0f, 0f, 0f, 0f));
			texture.Apply(true, false);
		}
	}
}
