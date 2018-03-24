using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
#if NETSTANDARD1_6 || NET451
using System.Buffers;
#endif
using Newtonsoft.Json;

namespace React.Core
{
	internal class ReactArrayPool<T> : IArrayPool<T>
	{
		public static ReactArrayPool<T> Instance = new ReactArrayPool<T>();

#if NETSTANDARD1_6 || NET451
		private readonly ArrayPool<T> _inner;

		public ReactArrayPool()
		{
			_inner = ArrayPool<T>.Shared;
		}

		public T[] Rent(int minimumLength)
		{
			return _inner.Rent(minimumLength);
		}

		public void Return(T[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			_inner.Return(array);
		}
#else
		private const int DefaultMaxArrayLength = 1024 * 1024;
		private const int DefaultMaxNumberOfArraysPerBucket = 50;
		private static T[] s_emptyArray;

		private readonly Bucket[] _buckets;


		/// <summary>
		/// Backport from System.Buffers
		/// </summary>
		internal ReactArrayPool() : this(DefaultMaxArrayLength, DefaultMaxNumberOfArraysPerBucket)
		{
		}

		internal ReactArrayPool(int maxArrayLength, int maxArraysPerBucket)
		{
			if (maxArrayLength <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxArrayLength));
			}
			if (maxArraysPerBucket <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxArraysPerBucket));
			}

			const int MinimumArrayLength = 0x10, MaximumArrayLength = 0x40000000;
			if (maxArrayLength > MaximumArrayLength)
			{
				maxArrayLength = MaximumArrayLength;
			}
			else if (maxArrayLength < MinimumArrayLength)
			{
				maxArrayLength = MinimumArrayLength;
			}

			// Create the buckets.
			int poolId = Id;
			int maxBuckets = Utilities.SelectBucketIndex(maxArrayLength);
			var buckets = new Bucket[maxBuckets + 1];
			for (int i = 0; i < buckets.Length; i++)
			{
				buckets[i] = new Bucket(Utilities.GetMaxSizeForBucket(i), maxArraysPerBucket, poolId);
			}
			_buckets = buckets;
		}

		private int Id => GetHashCode();

		public T[] Rent(int minimumLength)
		{
			if (minimumLength < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(minimumLength));
			}
			else if (minimumLength == 0)
			{
				return s_emptyArray ?? (s_emptyArray = new T[0]);
			}

			T[] buffer = null;

			int index = Utilities.SelectBucketIndex(minimumLength);
			if (index < _buckets.Length)
			{
				const int MaxBucketsToTry = 2;
				int i = index;
				do
				{
					// Attempt to rent from the bucket.  If we get a buffer from it, return it.
					buffer = _buckets[i].Rent();
					if (buffer != null)
					{
						return buffer;
					}
				}
				while (++i < _buckets.Length && i != index + MaxBucketsToTry);

				buffer = new T[_buckets[index]._bufferLength];
			}
			else
			{
				buffer = new T[minimumLength];
			}

			return buffer;
		}

		public void Return(T[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}
			else if (array.Length == 0)
			{
				return;
			}

			int bucket = Utilities.SelectBucketIndex(array.Length);

			if (bucket < _buckets.Length)
			{
				_buckets[bucket].Return(array);
			}
		}

		private sealed class Bucket
		{
			internal readonly int _bufferLength;
			private readonly T[][] _buffers;
			private readonly int _poolId;

			private SpinLock _lock;
			private int _index;

			internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
			{
				_lock = new SpinLock(Debugger.IsAttached);
				_buffers = new T[numberOfBuffers][];
				_bufferLength = bufferLength;
				_poolId = poolId;
			}

			internal int Id => GetHashCode();

			internal T[] Rent()
			{
				T[][] buffers = _buffers;
				T[] buffer = null;

				bool lockTaken = false, allocateBuffer = false;
				try
				{
					_lock.Enter(ref lockTaken);

					if (_index < buffers.Length)
					{
						buffer = buffers[_index];
						buffers[_index++] = null;
						allocateBuffer = buffer == null;
					}
				}
				finally
				{
					if (lockTaken) _lock.Exit(false);
				}

				if (allocateBuffer)
				{
					buffer = new T[_bufferLength];
				}

				return buffer;
			}

			internal void Return(T[] array)
			{
				if (array.Length != _bufferLength)
				{
					throw new ArgumentException("Buffer is not from pool");
				}

				bool lockTaken = false;
				try
				{
					_lock.Enter(ref lockTaken);

					if (_index != 0)
					{
						_buffers[--_index] = array;
					}
				}
				finally
				{
					if (lockTaken) _lock.Exit(false);
				}
			}
		}

		internal static class Utilities
		{
			internal static int SelectBucketIndex(int bufferSize)
			{
				uint bitsRemaining = ((uint)bufferSize - 1) >> 4;

				int poolIndex = 0;
				if (bitsRemaining > 0xFFFF) { bitsRemaining >>= 16; poolIndex = 16; }
				if (bitsRemaining > 0xFF) { bitsRemaining >>= 8; poolIndex += 8; }
				if (bitsRemaining > 0xF) { bitsRemaining >>= 4; poolIndex += 4; }
				if (bitsRemaining > 0x3) { bitsRemaining >>= 2; poolIndex += 2; }
				if (bitsRemaining > 0x1) { bitsRemaining >>= 1; poolIndex += 1; }

				return poolIndex + (int)bitsRemaining;
			}

			internal static int GetMaxSizeForBucket(int binIndex)
			{
				int maxSize = 16 << binIndex;
				return maxSize;
			}
		}
#endif
	}
}

