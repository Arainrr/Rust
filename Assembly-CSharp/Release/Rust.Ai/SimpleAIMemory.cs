using System.Collections.Generic;
using UnityEngine;

namespace Rust.AI
{
	public class SimpleAIMemory
	{
		public struct SeenInfo
		{
			public BaseEntity Entity;

			public Vector3 Position;

			public float Timestamp;

			public float Danger;
		}

		public List<BaseEntity> Visible = new List<BaseEntity>();

		public List<SeenInfo> All = new List<SeenInfo>();

		public void Update(BaseEntity ent)
		{
			for (int i = 0; i < All.Count; i++)
			{
				if (All[i].Entity == ent)
				{
					SeenInfo value = All[i];
					value.Position = ent.transform.position;
					value.Timestamp = Mathf.Max(Time.realtimeSinceStartup, value.Timestamp);
					All[i] = value;
					return;
				}
			}
			All.Add(new SeenInfo
			{
				Entity = ent,
				Position = ent.transform.position,
				Timestamp = Time.realtimeSinceStartup
			});
			Visible.Add(ent);
		}

		public void AddDanger(Vector3 position, float amount)
		{
			All.Add(new SeenInfo
			{
				Position = position,
				Timestamp = Time.realtimeSinceStartup,
				Danger = amount
			});
		}

		internal void Forget(float secondsOld)
		{
			for (int i = 0; i < All.Count; i++)
			{
				if (Time.realtimeSinceStartup - All[i].Timestamp > secondsOld)
				{
					if (All[i].Entity != null)
					{
						Visible.Remove(All[i].Entity);
					}
					All.RemoveAt(i);
					i--;
				}
			}
		}
	}
}
