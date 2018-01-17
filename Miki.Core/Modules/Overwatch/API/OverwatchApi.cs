using Miki.Modules.Overwatch.Objects;
using Rest;
using System.Threading.Tasks;

namespace Miki.Modules.Overwatch.API
{
    internal class OverwatchAPI
    {
        public static async Task<OverwatchUserResponse> GetUser(string Name, int Identifier)
        {
            RestResponse<OverwatchUserResponse> userdata = await new RestClient($"http://owapi.net/api/v3/u/{ Name }-{ Identifier }/blob")
                .AddHeader("user-agent", "velddev/miki")
                .GetAsync<OverwatchUserResponse>();

            if (userdata.Data == null)
            {
                return null;
            }

            return userdata.Data;
        }
    }
}