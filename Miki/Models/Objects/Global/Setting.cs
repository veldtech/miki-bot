using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Miki.Discord.Common;
using Miki.Modules;

namespace Miki.Models
{
	public enum DatabaseSettingId
    {
		LEVEL_NOTIFICATIONS = 0
    };

	[Table("Settings")]
	public class Setting
	{
		[Key]
		[Column("EntityId", Order = 0)]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long EntityId { get; set; }

		[Key]
		[Column("SettingId", Order = 1)]
		public DatabaseSettingId SettingId { get; set; }

		[Column("Value")]
		public int Value { get; set; }

		public static async Task<int> GetAsync(ulong id, DatabaseSettingId settingId)
		=> await GetAsync((long)id, settingId);
		public static async Task<int> GetAsync(long id, DatabaseSettingId settingId)
		{
			using (var context = new MikiContext())
			{
				Setting s = await context.Settings.FindAsync(id, settingId);
				if (s == null)
				{
					s = (await context.Settings.AddAsync(new Setting()
					{
						EntityId = id,
						SettingId = settingId,
						Value = 0
					})).Entity;
				}
				return s.Value;
			}
		}

		public static async Task<T> GetAsync<T>(long id, DatabaseSettingId settingId) where T : struct, IConvertible
			=> (T)(object)await GetAsync(id, settingId);
		public static async Task<T> GetAsync<T>(ulong id, DatabaseSettingId settingId) where T : struct, IConvertible
			=> (T)(object)await GetAsync((long)id, settingId);

		public static async Task UpdateAsync(long id, DatabaseSettingId settingId, int value)
		{
			using (var context = new MikiContext())
			{
				Setting s = await context.Settings.FindAsync(id, settingId);
				if (s == null)
				{
					await context.AddAsync(new Setting()
					{
						EntityId = id,
						SettingId = settingId,
						Value = value
					});
				}
				else
				{
					s.Value = value;
				}

				await context.SaveChangesAsync();
			}
		}
		public static async Task UpdateAsync(ulong id, DatabaseSettingId settingId, int value)
			=> await UpdateAsync((long)id, settingId, value);

		public static async Task UpdateGuildAsync(IDiscordGuild guild, DatabaseSettingId settingId, int newSetting)
		{
			var channels = await guild.GetChannelsAsync();
			foreach (var channel in channels)
			{
				await UpdateAsync((long)channel.Id, settingId, newSetting);
			}
		}

		private static string GetKey(long id, DatabaseSettingId setting)
			=> $"miki:settings:{id}:{(int)setting}";
	}
}