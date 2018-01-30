using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Utility.MML
{
	public class MMLObject
	{
		public string Key;
		public object Value;

		public MMLObject(string key, object value)
		{
			Key = key;
			Value = value;
		}
	}
}
