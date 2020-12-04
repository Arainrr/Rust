using System;
using System.IO;
using UnityEngine;

namespace ConVar
{
	[Factory("world")]
	public class World : ConsoleSystem
	{
		[ClientVar]
		[ServerVar]
		public static bool cache = true;

		[ClientVar]
		public static bool streaming = true;

		[ClientVar]
		public static void monuments(Arg arg)
		{
			if ((bool)TerrainMeta.Path)
			{
				TextTable textTable = new TextTable();
				textTable.AddColumn("type");
				textTable.AddColumn("name");
				textTable.AddColumn("pos");
				foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
				{
					textTable.AddRow(monument.Type.ToString(), monument.name, monument.transform.position.ToString());
				}
				arg.ReplyWith(textTable.ToString());
			}
		}

		[ServerVar(Clientside = true, Help = "Renders a high resolution PNG of the current map")]
		public static void rendermap(Arg arg)
		{
			float @float = arg.GetFloat(0, 1f);
			int imageWidth;
			int imageHeight;
			Color background;
			byte[] array = MapImageRenderer.Render(out imageWidth, out imageHeight, out background, @float, false);
			if (array == null)
			{
				arg.ReplyWith("Failed to render the map (is a map loaded now?)");
				return;
			}
			string fullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"map_{global::World.Size}_{global::World.Seed}.png"));
			File.WriteAllBytes(fullPath, array);
			arg.ReplyWith("Saved map render to: " + fullPath);
		}
	}
}
