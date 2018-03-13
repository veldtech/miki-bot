using ProtoBuf;
using System;
using System.Threading.Tasks;

namespace Miki.Models
{
	[ProtoContract]
    public class Achievement
    {
		[ProtoMember(1)]
        public long Id { get; set; }
		[ProtoMember(2)]
		public string Name { get; set; }
		[ProtoMember(3)]
		public short Rank { get; set; }
		[ProtoMember(4)]
		public DateTime UnlockedAt { get; set; }

		public User User { get; set; }

		public static async Task<Achievement> GetAsync(MikiContext context, long userId, string name)
		{
			string key = $"achievement:{userId}:{name}";

			if (await Global.redisClient.ExistsAsync(key))
			{
				Achievement a = await Global.redisClient.GetAsync<Achievement>(key);
				if(a != null)	
				{
					return context.Attach(a).Entity;
				}
			}			

			Achievement achievement = await context.Achievements.FindAsync(userId, name);
			await Global.redisClient.AddAsync(key, achievement);
			return achievement;
		}

		internal static async Task UpdateCacheAsync(long userId, string name, Achievement achievement)
		{
			string key = $"achievement:{userId}:{name}";

			await Global.redisClient.AddAsync(key, achievement);
		}
	}
}