using System;
using System.Collections.Generic;
using System.Text;

namespace Miki.API
{
    internal class Utils
    {
		public static StringBuilder CreateBaseRoute() =>
			new StringBuilder()
				.Append("/")
				.Append(MikiApi.API_VERSION);

		public static string AddToken(string token) => "?key=" + token;

	}
}
