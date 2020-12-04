namespace CCTVRender
{
	public struct Job
	{
		public uint NetId
		{
			get;
		}

		public uint RequestId
		{
			get;
		}

		public float Assigned
		{
			get;
		}

		public Job(uint netId, uint requestId, float assigned)
		{
			NetId = netId;
			RequestId = requestId;
			Assigned = assigned;
		}
	}
}
