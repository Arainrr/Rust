namespace CCTVRender
{
	public class ClientState
	{
		public ulong UserId
		{
			get;
			private set;
		}

		public float LastAssigned
		{
			get;
			set;
		}

		public void Initialize(ulong userId)
		{
			UserId = userId;
			LastAssigned = 0f;
		}
	}
}
