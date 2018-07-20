using Miki.Discord.Common;
using Miki.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Models.Objects.Guild
{
	[ProtoContract]
    public class BankAccount
    {
		[ProtoMember(1)]
		public long UserId { get; set; }
		
		[ProtoMember(2)]
		public long GuildId { get; set; }

		[ProtoMember(3)]
		public long Currency { get; set; }

		[ProtoMember(4)]
		public long TotalDeposited { get; set; }

		public static async Task<BankAccount> GetAsync(MikiContext context, IDiscordUser user, IDiscordGuild guild)
		{
			if (await Global.RedisClient.ExistsAsync($"bankaccount:{guild.Id}:{user.Id}"))
			{
				return context.BankAccounts.Attach(await Global.RedisClient.GetAsync<BankAccount>($"bankaccount:{guild.Id}:{user.Id}")).Entity;
			}
			BankAccount account = await context.BankAccounts.FindAsync(guild.Id.ToString(), user.Id.ToDbLong());
			await Global.RedisClient.UpsertAsync<BankAccount>($"bankaccount:{guild.Id}:{user.Id}", account);
			return account;
		}
    }
}
