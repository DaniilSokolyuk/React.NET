using System;
#if NETSTANDARD1_6 || NET451
using System.Buffers;
#endif
using Newtonsoft.Json;

namespace React.Core
{
	internal class ReactArrayPool<T> : IArrayPool<T>
	{
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
		public ReactArrayPool()
		{
		}

		public T[] Rent(int minimumLength)
		{
			return null;
		}

		public void Return(T[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}
		}
#endif
	}
}
