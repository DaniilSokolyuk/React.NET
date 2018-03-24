using System;
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
		private static readonly int bufferLength = 1024 * 1024;
		readonly object gate;
		int index;
		T[][] buffers;

		public ReactArrayPool()
		{
			buffers = new T[4][];
			gate = new object();
		}


		public T[] Rent(int minimumLength)
		{
			lock (gate)
			{
				if (index >= buffers.Length)
				{
					Array.Resize(ref buffers, buffers.Length * 2);
				}

				if (buffers[index] == null)
				{
					buffers[index] = new T[bufferLength];
				}

				var buffer = buffers[index];
				buffers[index] = null;
				index++;

				return buffer;
			}
		}

		public void Return(T[] array)
		{
			if (array.Length != bufferLength)
			{
				throw new InvalidOperationException("return buffer is not from pool");
			}

			lock (gate)
			{
				if (index != 0)
				{
					buffers[--index] = array;
				}
			}
		}
#endif
	}
}
