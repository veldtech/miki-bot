using System.Collections.Generic;
using System.Linq;

namespace Miki.Helpers
{
	public static class EnumerableHelper
	{
		public static T Splice<T, TType>(this T v, int size, int offset = 0)
			where T : IEnumerable<TType>
		{
			if(v.Count() > offset + size)
			{
				return (T)v.Skip(offset).Take(size);
			}
			return (T)v.Skip(offset).Take(v.Count() - offset);
		}
	}
}
