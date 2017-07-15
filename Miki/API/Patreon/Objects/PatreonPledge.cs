using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Patreon
{
    /* public class PatreonPledgeResponse
     {
         [JsonProperty("data")]
         public List<PatreonPledge> Data { get; set; } = new List<PatreonPledge>();

         [JsonProperty("included")]
         public List<PatreonEntity> Included { get; set; } = new List<PatreonEntity>();

         [JsonProperty("links")]
         public Dictionary<string, string> Links { get; set; } = new Dictionary<string, string>();

         [JsonProperty("meta")]
         public Dictionary<string, object> Meta { get; set; } = new Dictionary<string, object>();
     }

     public class PatreonEntity
     {
         [JsonProperty("attributes")]
         public object[] Attributes { get; set; }

         [JsonProperty("id")]
         public string Id { get; set; }

         [JsonProperty("relationships")]
         public PatreonRelationships Relationships { get; set; }

         [JsonProperty("type")]
         public string Type { get; set; }
     }

     public class PatreonCampaign : PatreonEntity
     {
         [JsonProperty("attributes")]
         public PatreonCampaignAttribute Attributes { get; set; }    

     }

     public class PatreonPledge : PatreonEntity
     {
      //   [JsonProperty("attributes")]
    //     public PatreonPledgeAttribute Attributes { get; set; }
     }

     public class PatreonPledgeAttribute
     {
         [JsonProperty("amount_cents")]
         public int AmountCents { get; set; }

         [JsonProperty("created_at")]
         public DateTime CreatedAt { get; set; }

         [JsonProperty("pledge_cap_cents")]
         public int PatronPledgeCapCents { get; set; }

         [JsonProperty("patron_pays_fees")]
         public bool PatreonPaidFeed { get; set; }
     }

     public class PatreonCampaignAttribute
     {
         [JsonProperty("created_at")]
         public DateTime CreatedAt { get; set; }

         [JsonProperty("creation_count")]
         public int CreationCount { get; set; }

         [JsonProperty("creation_name")]
         public string CreationName { get; set; }

         [JsonProperty("discord_server_id")]
         public string DiscordServerId { get; set; }

         [JsonProperty("display_patron_goals")]
         public bool DisplayPatreonGoals { get; set; }

         [JsonProperty("earnings_visibility")]
         public string EarningsVisibility { get; set; }

         [JsonProperty("image_small_url")]
         public string SmallImageUrl { get; set; }

         [JsonProperty("image_url")]
         public string ImageUrl { get; set; }

         [JsonProperty("is_charged_immediately")]
         public bool ChargedImmediately { get; set; }

         [JsonProperty("is_monthly")]
         public bool ChargedMonthly { get; set; }

         [JsonProperty("is_nsfw")]
         public bool IsNsfw { get; set; }

         [JsonProperty("is_plural")]
         public bool IsPlural { get; set; }

         [JsonProperty("main_video_embed")]
         public string MainVideoEmbed { get; set; }

         [JsonProperty("main_video_url")]
         public string MainVideoUrl { get; set; }

         [JsonProperty("one_liner")]
         public string OneLiner { get; set; }

         [JsonProperty("outstanding_payment_amount_cents")]
         public int OutstandingPaymentAmountCents { get; set; }

         [JsonProperty("patron_count")]
         public int PatronCount { get; set; }

         [JsonProperty("pay_per_name")]
         public string PayPerName { get; set; }

         [JsonProperty("pledge_sum")]
         public int PledgeSum { get; set; }

         [JsonProperty("pledge_url")]
         public string PledgeUrl { get; set; }

         [JsonProperty("published_at")]
         public DateTime PublishedAt { get; set; }

         [JsonProperty("summary")]
         public string Summary { get; set; }

         [JsonProperty("thanks_embed")]
         public string ThanksEmbed { get; set; }

         [JsonProperty("thanks_msg")]
         public string ThanksMessage { get; set; }

         [JsonProperty("thanks_video_url")]
         public string ThanksVideoUrl { get; set; }
     }

     public class PatreonRelationships
     {

     }

     public class PatreonRelationshipItem
     {
         [JsonProperty("data")]
         public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

         [JsonProperty("links")]
         public Dictionary<string, string> Links { get; set; } = new Dictionary<string, string>();
     }

     public class PatreonRelationshipItemData
     {
         [JsonProperty("id")]
         public string Id { get; set; }

         [JsonProperty("type")]
         public string Type { get; set; }
     }

     public class PatreonUser : PatreonEntity
     {
  //       [JsonProperty("attributes")]
   //      public PatreonUserAttributes Attributes { get; set; }
     }

     public class PatreonUserAttributes
     {
         [JsonProperty("about")]
         public string About { get; set; }

         [JsonProperty("created_at")]
         public DateTime CreatedAt { get; set; }

         [JsonProperty("email")]
         public string EmailAddress { get; set; }

         [JsonProperty("facebook")]
         public string FacebookUrl { get; set; }

         [JsonProperty("first_name")]
         public string FirstName { get; set; }

         [JsonProperty("full_name")]
         public string FullName { get; set; }

         [JsonProperty("gender")]
         public int Gender { get; set; }

         [JsonProperty("image_url")]
         public string ImageUrl { get; set; }

         [JsonProperty("is_email_verified")]
         public bool IsEmailVerified { get; set; }

         [JsonProperty("last_name")]
         public string LastName { get; set; }

         [JsonProperty("social_connections")]
         public Dictionary<string, Dictionary<string, string>> SocialConnections { get; set; } = new Dictionary<string, Dictionary<string, string>>();

         [JsonProperty("thumb_url")]
         public string ThumbnailUrl { get; set; }

         [JsonProperty("twitch")]
         public string TwitchUrl { get; set; }

         [JsonProperty("twitter")]
         public string TwitterUrl { get; set; }

         [JsonProperty("url")]
         public string PatreonUrl { get; set; }

         [JsonProperty("vanity")]
         public string Vanity { get; set; }

         [JsonProperty("youtube")]
         public string YoutubeUrl { get; set; }
     }

     public class PatreonReward
     {

     }

     public class PatreonAddress
     {
     }

     public class PatreonCard
     {

     }*/

    public class Attributes
    {
        public int amount_cents { get; set; }
        public string created_at { get; set; }
        public string declined_since { get; set; }
        public bool patron_pays_fees { get; set; }
        public int pledge_cap_cents { get; set; }
    }

    public class Address
    {
        public object data { get; set; }
    }

    public class Data
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links
    {
        public string related { get; set; }
    }

    public class Creator
    {
        public Data data { get; set; }
        public Links links { get; set; }
    }

    public class Data2
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links2
    {
        public string related { get; set; }
    }

    public class Patron
    {
        public Data2 data { get; set; }
        public Links2 links { get; set; }
    }

    public class Data3
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links3
    {
        public string related { get; set; }
    }

    public class Reward
    {
        public Data3 data { get; set; }
        public Links3 links { get; set; }
    }

    public class Relationships
    {
        public Address address { get; set; }
        public Creator creator { get; set; }
        public Patron patron { get; set; }
        public Reward reward { get; set; }
    }

    public class Datum
    {
        public Attributes attributes { get; set; }
        public string id { get; set; }
        public Relationships relationships { get; set; }
        public string type { get; set; }
    }

    public class Discord
    {
        public string user_id { get; set; }
        public List<string> scopes { get; set; }
    }

    public class SocialConnections
    {
        public object deviantart { get; set; }
        public Discord discord { get; set; }
        public object facebook { get; set; }
        public object spotify { get; set; }
        public object twitch { get; set; }
        public object twitter { get; set; }
        public object youtube { get; set; }
    }

    public class Attributes2
    {
        public string about { get; set; }
        public string created { get; set; }
        public string email { get; set; }
        public string facebook { get; set; }
        public string first_name { get; set; }
        public string full_name { get; set; }
        public int gender { get; set; }
        public string image_url { get; set; }
        public bool is_email_verified { get; set; }
        public string last_name { get; set; }
        public SocialConnections social_connections { get; set; }
        public string thumb_url { get; set; }
        public string twitch { get; set; }
        public string twitter { get; set; }
        public string url { get; set; }
        public string vanity { get; set; }
        public string youtube { get; set; }
        public int? amount { get; set; }
        public int? amount_cents { get; set; }
        public string created_at { get; set; }
        public object deleted_at { get; set; }
        public string description { get; set; }
        public List<string> discord_role_ids { get; set; }
        public string edited_at { get; set; }
        public int? patron_count { get; set; }
        public int? post_count { get; set; }
        public bool? published { get; set; }
        public string published_at { get; set; }
        public int? remaining { get; set; }
        public bool? requires_shipping { get; set; }
        public string title { get; set; }
        public object unpublished_at { get; set; }
        public object user_limit { get; set; }
        public string discord_id { get; set; }
        public object facebook_id { get; set; }
        public bool? has_password { get; set; }
        public bool? is_deleted { get; set; }
        public bool? is_nuked { get; set; }
        public bool? is_suspended { get; set; }
        public int? creation_count { get; set; }
        public string creation_name { get; set; }
        public string discord_server_id { get; set; }
        public bool? display_patron_goals { get; set; }
        public object earnings_visibility { get; set; }
        public string image_small_url { get; set; }
        public bool? is_charged_immediately { get; set; }
        public bool? is_monthly { get; set; }
        public bool? is_nsfw { get; set; }
        public bool? is_plural { get; set; }
        public string main_video_embed { get; set; }
        public string main_video_url { get; set; }
        public object one_liner { get; set; }
        public int? outstanding_payment_amount_cents { get; set; }
        public string pay_per_name { get; set; }
        public int? pledge_sum { get; set; }
        public string pledge_url { get; set; }
        public string summary { get; set; }
        public string thanks_embed { get; set; }
        public string thanks_msg { get; set; }
        public object thanks_video_url { get; set; }
        public string type { get; set; }
        public int? completed_percentage { get; set; }
        public string reached_at { get; set; }
    }

    public class Data4
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links4
    {
        public string related { get; set; }
    }

    public class Campaign
    {
        public Data4 data { get; set; }
        public Links4 links { get; set; }
    }

    public class Data5
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links5
    {
        public string related { get; set; }
    }

    public class Creator2
    {
        public Data5 data { get; set; }
        public Links5 links { get; set; }
    }

    public class Goals
    {
        public List<object> data { get; set; }
    }

    public class Datum2
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Rewards
    {
        public List<Datum2> data { get; set; }
    }

    public class Relationships2
    {
        public Campaign campaign { get; set; }
        public Creator2 creator { get; set; }
        public Goals goals { get; set; }
        public Rewards rewards { get; set; }
    }

    public class Included
    {
        public Attributes2 attributes { get; set; }
        public string id { get; set; }
        public Relationships2 relationships { get; set; }
        public string type { get; set; }
    }

    public class Links6
    {
        public string first { get; set; }
        public string next { get; set; }
    }

    public class Meta
    {
        public int count { get; set; }
    }

    public class RootObject
    {
        public List<Datum> data { get; set; }
        public List<Included> included { get; set; }
        public Links6 links { get; set; }
        public Meta meta { get; set; }
    }
}
