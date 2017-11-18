using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Miki
{
	class MikiApi
	{
		string token = "";
		string baseUrl = "";

		public MikiApi(string base_url, string token)
		{
			this.token = token;
			baseUrl = base_url;
		}

		public async Task GenerateStickerImageAsync(ulong userid)
		{

		}
	}
}
