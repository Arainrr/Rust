using UnityEngine;

public class CCTVRenderController : SingletonComponent<CCTVRenderController>
{
	public Camera Camera;

	[Range(320f, 1280f)]
	public int Width = 854;

	[Range(240f, 720f)]
	public int Height = 480;

	[Range(1f, 100f)]
	public int Quality = 75;

	private RenderTexture _renderTexture;

	private Texture2D _texture;

	public void OnEnable()
	{
		_renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
		_texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
		Camera.enabled = false;
		Camera.forceIntoRenderTexture = true;
		Camera.targetTexture = _renderTexture;
	}

	public void OnDisable()
	{
		Object.Destroy(_renderTexture);
		Object.Destroy(_texture);
		_renderTexture = null;
		_texture = null;
	}
}
