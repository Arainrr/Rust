using Facepunch;
using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace CCTVRender
{
	public class RenderState : Pool.IPooled
	{
		private float _lastRendered;

		private HashSet<JobReceiver> _receivers;

		private byte[] _imageCache;

		private int _imageCacheLength;

		private float _imageCacheTime;

		public uint NetId
		{
			get;
			private set;
		}

		public bool IsLocked
		{
			get;
			private set;
		}

		public uint Frame
		{
			get;
			private set;
		}

		public bool WasRecentlyRendered => Time.realtimeSinceStartup - _lastRendered < 1f;

		public bool HasCachedFrame
		{
			get
			{
				if (_imageCache != null && _imageCacheLength > 0)
				{
					return Time.realtimeSinceStartup - _imageCacheTime < 5f;
				}
				return false;
			}
		}

		public ArraySegment<byte> CachedFrame => new ArraySegment<byte>(_imageCache, 0, _imageCacheLength);

		public void Initialize(uint netId)
		{
			NetId = netId;
			IsLocked = false;
			Frame = 0u;
			_lastRendered = 0f;
			_receivers = Pool.Get<HashSet<JobReceiver>>();
			_receivers.Clear();
			_imageCache = ArrayPool<byte>.Shared.Rent(153600);
			_imageCacheLength = 0;
		}

		public void EnterPool()
		{
			if (_receivers != null)
			{
				_receivers.Clear();
				Pool.Free(ref _receivers);
			}
			if (_imageCache != null)
			{
				ArrayPool<byte>.Shared.Return(_imageCache);
				_imageCache = null;
			}
		}

		public bool AddReceiver(JobReceiver receiver)
		{
			if (!IsLocked)
			{
				DebugEx.LogWarning("Adding receiver when the state isn't locked!");
			}
			return _receivers.Add(receiver);
		}

		public void BeginRequest()
		{
			if (IsLocked)
			{
				DebugEx.LogWarning("Beginning request while locked! Aborting previous");
				AbortRequest();
			}
			IsLocked = true;
		}

		public void CompleteRequest(Span<byte> jpgImage)
		{
			jpgImage.CopyTo(_imageCache);
			_imageCacheLength = jpgImage.Length;
			_imageCacheTime = Time.realtimeSinceStartup;
			Frame++;
			foreach (JobReceiver receiver in _receivers)
			{
				try
				{
					receiver.Receiver.RenderCompleted(receiver.RequestId, Frame, jpgImage);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
			_receivers.Clear();
			IsLocked = false;
			_lastRendered = Time.realtimeSinceStartup;
		}

		public void AbortRequest()
		{
			_receivers.Clear();
			IsLocked = false;
		}

		public void LeavePool()
		{
		}
	}
}
