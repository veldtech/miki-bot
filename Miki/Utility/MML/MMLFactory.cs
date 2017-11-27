using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility.MML
{
	public class MMLFactory
	{
		public static MMLObject Create(string query)
		{
			List<string> seperatedList = query.TrimStart('-').Split(':').ToList();
			if (seperatedList.Count == 1)
			{
				return new MMLObject(seperatedList[0].ToLower(), true);
			}
			else if (seperatedList.Count == 2)
			{
				return new MMLObject(seperatedList[0].ToLower(), Parse(seperatedList[1]));
			}
			return new MMLObject("error", null);
		}

		private static object Parse(string val)
		{
			if(val.StartsWith("\""))
			{
				return val.TrimStart('"').TrimEnd('"');
			}
			else if (int.TryParse(val, out int v))
			{
				return v;
			}
			else if (Utils.ToBool(val))
			{
				return Utils.ToBool(val);
			}
			return val.ToString();
		}
	}
}
