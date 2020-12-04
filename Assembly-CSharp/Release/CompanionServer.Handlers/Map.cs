using Facepunch;
using ProtoBuf;
using System;
using UnityEngine;

namespace CompanionServer.Handlers
{
	public class Map : BaseHandler<AppEmpty>
	{
		private static int _width;

		private static int _height;

		private static byte[] _imageData;

		private static string _background;

		protected override int TokenCost => 5;

		public override void Execute()
		{
			if (_imageData == null)
			{
				SendError("no_map");
				return;
			}
			AppMap appMap = Pool.Get<AppMap>();
			appMap.width = (uint)_width;
			appMap.height = (uint)_height;
			appMap.oceanMargin = 500;
			appMap.jpgImage = _imageData;
			appMap.background = _background;
			appMap.monuments = Pool.GetList<AppMap.Monument>();
			if (TerrainMeta.Path != null && TerrainMeta.Path.Monuments != null)
			{
				foreach (MonumentInfo monument2 in TerrainMeta.Path.Monuments)
				{
					if (monument2.shouldDisplayOnMap)
					{
						Vector2 vector = Util.WorldToMap(monument2.transform.position);
						AppMap.Monument monument = Pool.Get<AppMap.Monument>();
						monument.token = (monument2.displayPhrase.IsValid() ? monument2.displayPhrase.token : monument2.transform.root.name);
						monument.x = vector.x;
						monument.y = vector.y;
						appMap.monuments.Add(monument);
					}
				}
			}
			AppResponse appResponse = Pool.Get<AppResponse>();
			appResponse.map = appMap;
			Send(appResponse);
		}

		public static void PopulateCache()
		{
			RenderToCache();
		}

		private static void RenderToCache()
		{
			_imageData = null;
			_width = 0;
			_height = 0;
			try
			{
				Color background;
				_imageData = MapImageRenderer.Render(out _width, out _height, out background);
				_background = "#" + ColorUtility.ToHtmlStringRGB(background);
			}
			catch (Exception arg)
			{
				Debug.LogError($"Exception thrown when rendering map for the app: {arg}");
			}
			if (_imageData == null)
			{
				Debug.LogError("Map image is null! App users will not be able to see the map.");
			}
		}
	}
}