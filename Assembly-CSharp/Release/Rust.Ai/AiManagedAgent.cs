using System;
using UnityEngine;

namespace Rust.Ai
{
	[DefaultExecutionOrder(-102)]
	public class AiManagedAgent : FacepunchBehaviour, IServerComponent
	{
		[Tooltip("TODO: Replace with actual agent type id on the NavMeshAgent when we upgrade to 5.6.1 or above.")]
		public int AgentTypeIndex;

		[NonSerialized]
		[ReadOnly]
		public Vector2i NavmeshGridCoord;

		private IAIAgent agent;

		private bool isRegistered;

		private void OnEnable()
		{
			isRegistered = false;
			if (SingletonComponent<AiManager>.Instance == null || !SingletonComponent<AiManager>.Instance.enabled || AiManager.nav_disable)
			{
				base.enabled = false;
				return;
			}
			agent = GetComponent<IAIAgent>();
			if (agent != null)
			{
				if (agent.Entity.isClient)
				{
					base.enabled = false;
					return;
				}
				agent.AgentTypeIndex = AgentTypeIndex;
				float num = SeedRandom.Value((uint)Mathf.Abs(GetInstanceID()));
				Invoke(DelayedRegistration, num * 3f);
			}
		}

		private void DelayedRegistration()
		{
			if (!isRegistered)
			{
				SingletonComponent<AiManager>.Instance.Add(agent);
				isRegistered = true;
			}
		}

		private void OnDisable()
		{
			if (!Application.isQuitting && !(SingletonComponent<AiManager>.Instance == null) && SingletonComponent<AiManager>.Instance.enabled && agent != null && !(agent.Entity == null) && !agent.Entity.isClient && isRegistered)
			{
				SingletonComponent<AiManager>.Instance.Remove(agent);
			}
		}
	}
}
