/*
 *  Copyright (c) 2014-Present, Facebook, Inc.
 *  All rights reserved.
 *
 *  This source code is licensed under the BSD-style license found in the
 *  LICENSE file in the root directory of this source tree. An additional grant
 *  of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using System.IO;

#if LEGACYASPNET
using System.Text;
using System.Web;
#else
using System.Text.Encodings.Web;
using IHtmlString = Microsoft.AspNetCore.Html.IHtmlContent;
#endif

#if LEGACYASPNET
namespace React.Web.Mvc
#else
namespace React.AspNet
#endif
{
	public class ActionHtmlString : IHtmlString
	{
		private readonly Action<TextWriter> _textWriter;

		public ActionHtmlString(Action<TextWriter> textWriter)
		{
			_textWriter = textWriter;
		}

#if LEGACYASPNET
		[ThreadStatic] 
		private static StringWriter _sharedStringWriter;

		public string ToHtmlString()
		{
			var stringWriter = _sharedStringWriter;
			if (stringWriter != null)
			{
				stringWriter.GetStringBuilder().Clear();
			}
			else
			{
				_sharedStringWriter = stringWriter = new StringWriter(new StringBuilder(512));
			}

			_textWriter(stringWriter);
			return stringWriter.ToString();
		}
#else
		public void WriteTo(TextWriter writer, HtmlEncoder encoder)
		{
			_textWriter(writer);
		}
#endif
	}
}
