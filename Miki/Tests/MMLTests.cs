using Miki.Utility.MML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Tests
{
	public class MMLTests : TestCase
	{
		[Test]
		public void MMLTestBig()
		{
			Dictionary<string, object> testDict = new MMLParser("-aa:ab -bc:12, -yes:no -no:yes -num:123")
				.Parse()
				.ToDictionary(x => x.Key, x => x.Value);

			Debug.Assert("ab" == (string)testDict["aa"], "MML string failed");
			Debug.Assert(12 == (int)testDict["bc"], "MML Test int Failed"); ;
			Debug.Assert(!(bool)testDict["no"], "MML Test Big Failed");

		}
	}
}
