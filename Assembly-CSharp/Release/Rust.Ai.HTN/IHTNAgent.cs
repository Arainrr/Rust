using UnityEngine;

namespace Rust.Ai.HTN
{
	public interface IHTNAgent
	{
		HTNDomain AiDomain
		{
			get;
		}

		BaseNpcDefinition AiDefinition
		{
			get;
		}

		bool IsDormant
		{
			get;
			set;
		}

		bool IsDestroyed
		{
			get;
		}

		BaseEntity Body
		{
			get;
		}

		Vector3 BodyPosition
		{
			get;
		}

		Vector3 EyePosition
		{
			get;
		}

		Quaternion EyeRotation
		{
			get;
		}

		BaseEntity MainTarget
		{
			get;
		}

		BaseNpc.AiStatistics.FamilyEnum Family
		{
			get;
		}

		Transform transform
		{
			get;
		}

		float healthFraction
		{
			get;
		}

		Vector3 estimatedVelocity
		{
			get;
		}
	}
}
