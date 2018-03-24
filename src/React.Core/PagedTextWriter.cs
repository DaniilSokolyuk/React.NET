using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace React.Core
{
	public class PagedPooledTextWriter : TextWriter
	{
		private readonly ArrayPool<char> _pool;

		public PagedPooledTextWriter()
		{
			_pool = ArrayPool<char>.Shared;
		}

		public const int PageSize = 1024;

		private int _charIndex;

		private IList<char[]> pages { get; } = new List<char[]>();

		private char[] CurrentPage { get; set; }

		public int Length
		{
			get
			{
				var length = _charIndex;
				for (var i = 0; i < pages.Count - 1; i++)
				{
					length += pages[i].Length;
				}

				return length;
			}
		}

		public void Clear()
		{
			for (var i = pages.Count - 1; i > 0; i--)
			{
				var page = pages[i];

				try
				{
					pages.RemoveAt(i);
				}
				finally
				{
					_pool.Return(page);
				}
			}

			_charIndex = 0;
			CurrentPage = pages.Count > 0 ? pages[0] : null;
		}

		public void WriteTo(TextWriter writer)
		{
			var length = Length;
			if (length == 0)
			{
				return;
			}

			for (var i = 0; i < pages.Count; i++)
			{
				var page = pages[i];
				var pageLength = Math.Min(length, page.Length);
				if (pageLength != 0)
				{
					writer.Write(page, index: 0, count: pageLength);
				}

				length -= pageLength;
			}
		}

		public override Encoding Encoding { get; }

		public override void Write(char value)
		{
			var page = GetCurrentPage();
			page[_charIndex++] = value;
		}

		public override void Write(char[] buffer)
		{
			if (buffer == null)
			{
				return;
			}

			Write(buffer, 0, buffer.Length);
		}

		public override void Write(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			while (count > 0)
			{
				var page = GetCurrentPage();
				var copyLength = Math.Min(count, page.Length - _charIndex);

				Array.Copy(
					buffer,
					index,
					page,
					_charIndex,
					copyLength);

				_charIndex += copyLength;
				index += copyLength;
				count -= copyLength;
			}
		}

		public override void Write(string value)
		{
			if (value == null)
			{
				return;
			}

			if (value == null)
			{
				return;
			}

			var index = 0;
			var count = value.Length;

			while (count > 0)
			{
				var page = GetCurrentPage();
				var copyLength = Math.Min(count, page.Length - _charIndex);

				value.CopyTo(
					index,
					page,
					_charIndex,
					copyLength);

				_charIndex += copyLength;
				index += copyLength;

				count -= copyLength;
			}
		}


		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			for (var i = 0; i < pages.Count; i++)
			{
				_pool.Return(pages[i]);
			}

			pages.Clear();
		}

		private char[] GetCurrentPage()
		{
			if (CurrentPage == null || _charIndex == CurrentPage.Length)
			{
				CurrentPage = NewPage();
				_charIndex = 0;
			}

			return CurrentPage;
		}

		private char[] NewPage()
		{
			char[] page = null;
			try
			{
				page = _pool.Rent(PageSize);
				pages.Add(page);
			}
			catch when (page != null)
			{
				_pool.Return(page);
				throw;
			}

			return page;
		}
	}
}
