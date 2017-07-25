using Miki.Modules.Overwatch.Objects;
using Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch.API
{
    class OverwatchApi
    {
        public async Task<OverwatchUserResponse> GetUser(string Name, int Identifier)
        {
            RestResponse<OverwatchUserResponse> userdata = await new RestClient($"https://owapi.net/api/v3/u/{ Name }-{ Identifier }/blob")
                .GetAsync<OverwatchUserResponse>();

            if(userdata.Data == null)
            {
                return null;
            }

        }
    }
}
