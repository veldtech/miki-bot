using Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Patreon
{
    class PatreonApi
    {
        string accessToken = "";

        List<Included> Patrons { get; set; } = new List<Included>();
        List<Datum> Pledges { get; set; } = new List<Datum>();


        public PatreonApi(string accessToken)
        {
            this.accessToken = accessToken;


        }

        public async Task<RootObject> GetAllPledgesAsync()
        {
            RestClient rc = new RestClient("https://api.patreon.com/oauth2/api/campaigns/240974/pledges");
            rc.SetAuthorisation("Bearer", accessToken);
            RestResponse<RootObject> pledges = await rc.GetAsync<RootObject>();

            if(pledges.Success)
            {
                return pledges.Data;
            }
            return null;
        }

        public async Task<int> GetDonationAmountById(ulong id)
        {
            RootObject o = await GetAllPledgesAsync();
            List<Datum> pledges = o.data.Where(p => p.type == "pledge").ToList();
            List<Included> users = o.included.Where(p => p.type == "user").ToList();

            Included user = users.Where(p => p.attributes.social_connections.discord.user_id == id.ToString()).FirstOrDefault();

            if (user != null)
            {
                Datum pledge = pledges.Where(p => p.relationships.patron.data.id == user.id).FirstOrDefault();

                if (pledge == null) return 0;
                if(DateTime.Parse(pledge.attributes.declined_since).AddMonths(1) <= DateTime.Now)
                {
                    return pledge.attributes.amount_cents;
                }
            }
            return 0;
        }
    }
}
