using Facepunch;
using System.Collections.Generic;

namespace CompanionServer
{
	public class TokenBucketList<TKey> : ITokenBucketSettings
	{
		private readonly Dictionary<TKey, TokenBucket> _buckets;

		public double MaxTokens
		{
			get;
		}

		public double TokensPerSec
		{
			get;
		}

		public TokenBucketList(double maxTokens, double tokensPerSec)
		{
			_buckets = new Dictionary<TKey, TokenBucket>();
			MaxTokens = maxTokens;
			TokensPerSec = tokensPerSec;
		}

		public TokenBucket Get(TKey key)
		{
			TokenBucket value;
			if (_buckets.TryGetValue(key, out value))
			{
				return value;
			}
			TokenBucket tokenBucket = Pool.Get<TokenBucket>();
			tokenBucket.Settings = this;
			tokenBucket.Reset();
			_buckets.Add(key, tokenBucket);
			return tokenBucket;
		}

		public void Cleanup()
		{
			List<TKey> obj = Pool.GetList<TKey>();
			foreach (KeyValuePair<TKey, TokenBucket> bucket in _buckets)
			{
				if (bucket.Value.IsFull)
				{
					obj.Add(bucket.Key);
				}
			}
			foreach (TKey item in obj)
			{
				TokenBucket value;
				if (_buckets.TryGetValue(item, out value))
				{
					Pool.Free(ref value);
					_buckets.Remove(item);
				}
			}
			Pool.FreeList(ref obj);
		}
	}
}
