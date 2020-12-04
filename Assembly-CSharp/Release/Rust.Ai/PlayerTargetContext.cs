using Apex.AI;
using UnityEngine;

namespace Rust.Ai
{
	public class PlayerTargetContext : IAIContext
	{
		public IAIAgent Self;

		public int CurrentOptionsIndex;

		public int PlayerCount;

		public BasePlayer[] Players;

		public Vector3[] Direction;

		public float[] Dot;

		public float[] DistanceSqr;

		public byte[] LineOfSight;

		public BasePlayer Target;

		public float Score;

		public int Index;

		public Vector3 LastKnownPosition;

		public void Refresh(IAIAgent self, BasePlayer[] players, int playerCount)
		{
			Self = self;
			Players = players;
			PlayerCount = playerCount;
			Target = null;
			Score = 0f;
			Index = -1;
			LastKnownPosition = Vector3.zero;
		}
	}
}
