using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Tests
{
	public class TestCase
	{
		// fix this
		public static void Run()
		{
			List<Type> types = Assembly.GetEntryAssembly().GetTypes().Where(x => x.IsAssignableFrom(typeof(TestCase))).ToList();
			foreach(var x in types)
			{
				foreach (var y in x.GetMethods().Where(z => z.GetCustomAttribute<TestAttribute>() != null))
				{
					y.Invoke(Activator.CreateInstance(x), null);
				}
			}
		}
	}
}
