using Miki.Discord;
using Miki.Discord.Rest;
using Miki.Dsl;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Framework.Languages;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	public enum LevelNotificationsSetting
	{
		REWARDS_ONLY = 0,
		ALL = 1,
		NONE = 2
	}

	[Module(Name = "settings")]
	internal class SettingsModule
	{
		[Command(Name = "setnotifications", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetupNotifications(EventContext e)
		{
			MMLParser mml = new MMLParser(e.Arguments.ToString());
			MSLResponse response = mml.Parse();

			bool global = response.GetBool("g");
			LevelNotificationsSetting type = Enum.Parse<LevelNotificationsSetting>(response.GetString("type"), true);

			using (MikiContext context = new MikiContext())
			{
				await Setting.UpdateAsync(context, e.Channel.Id, DatabaseSettingId.LEVEL_NOTIFICATIONS, (int)type);
				await context.SaveChangesAsync();
			}
		}

		//public async Task SetupNotificationsInteractive<T>(EventContext e, DatabaseSettingId settingId)
		//{
		//	List<string> options = Enum.GetNames(typeof(T))
		//		.Select(x => x.ToLower()
		//			.Replace('_', ' '))
		//		.ToList();

		//	string settingName = settingId.ToString().ToLower().Replace('_', ' ');

		//	var sEmbed= SettingsBaseEmbed;
		//	sEmbed.Description = ($"What kind of {settingName} do you want");
		//	sEmbed.AddInlineField("Options", string.Join("\n", options));
		//	var sMsg = await sEmbed.ToEmbed().SendToChannel(e.Channel);

		//	int newSetting;

		//	IDiscordMessage msg = null;

		//	while (true)
		//	{
		//		msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

		//		if (Enum.TryParse<LevelNotificationsSetting>(msg.Content.Replace(" ", "_"), true, out var setting))
		//		{
		//			newSetting = (int)setting;
		//			break;
		//		}

		//		await sMsg.EditAsync(new EditMessageArgs()
		//		{
		//			embed = e.ErrorEmbed("Oh, that didn't seem right! Try again")
		//				.AddInlineField("Options", string.Join("\n", options))
		//				.ToEmbed()
		//		});
		//	}

		//	sMsg = await SettingsBaseEmbed
		//		.SetDescription("Do you want this to apply for every channel? say `yes` if you do.")
		//		.ToEmbed().SendToChannel(e.Channel as IDiscordGuildChannel);

		//	msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
		//	bool global = (msg.Content.ToLower()[0] == 'y');

		//	await SettingsBaseEmbed
		//		.SetDescription($"Setting `{settingName}` Updated!")
		//		.ToEmbed().SendToChannel(e.Channel as IDiscordGuildChannel);

		//	if (!global)
		//	{
		//		using (var context = new MikiContext())
		//		{
		//			await Setting.UpdateAsync(context, e.Channel.Id, settingId, newSetting);
		//			await context.SaveChangesAsync();
		//		}
		//	}
		//	else
		//	{
		//		//await Setting.UpdateGuildAsync(e.Guild, settingId, newSetting);
		//	}
		//}

		[Command(Name = "showmodule")]
		public async Task ConfigAsync(EventContext e)
		{
			Module module = e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.FirstOrDefault(x => x.Name.ToLower() == e.Arguments.ToString().ToLower());

			if (module != null)
			{
				EmbedBuilder embed = new EmbedBuilder();

				embed.Title = (e.Arguments.ToString().ToUpper());

				string content = "";

				foreach (CommandEvent ev in module.Events.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(Global.RedisClient, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				embed.AddInlineField("Events", content);

				content = "";

				foreach (BaseService ev in module.Services.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(Global.RedisClient, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				if (!string.IsNullOrEmpty(content))
					embed.AddInlineField("Services", content);

				embed.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "showmodules")]
		public async Task ShowModulesAsync(EventContext e)
		{
			List<string> modules = new List<string>();
			SimpleCommandHandler commandHandler = e.EventSystem.GetCommandHandler<SimpleCommandHandler>();
			EventAccessibility userEventAccessibility = await commandHandler.GetUserAccessibility(e);

			foreach (CommandEvent ev in commandHandler.Commands)
			{
				if (userEventAccessibility >= ev.Accessibility)
				{
					if (ev.Module != null && !modules.Contains(ev.Module.Name.ToUpper()))
					{
						modules.Add(ev.Module.Name.ToUpper());
					}
				}
			}

			modules.Sort();

			string firstColumn = "", secondColumn = "";

			for (int i = 0; i < modules.Count(); i++)
			{
				string output = $"{(await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules[i].IsEnabled(Global.RedisClient, e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>")} {modules[i]}\n";
				if (i < modules.Count() / 2 + 1)
				{
					firstColumn += output;
				}
				else
				{
					secondColumn += output;
				}
			}

			new EmbedBuilder()
				.SetTitle($"Module Status for '{e.Channel.Name}'")
				.AddInlineField("Column 1", firstColumn)
				.AddInlineField("Column 2", secondColumn)
				.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetLocale(EventContext e)
		{
			string localeName = e.Arguments.ToString() ?? "";

			if (Locale.LocaleNames.TryGetValue(localeName, out string langId))
			{
				using (var context = new MikiContext())
					await Locale.SetLanguageAsync(context, e.Channel.Id, langId);

				e.SuccessEmbed(e.Locale.GetString("localization_set", $"`{localeName}`"))
					.QueueToChannel(e.Channel);

				return;
			}
			e.ErrorEmbedResource("error_language_invalid", 
				localeName, 
				await e.Prefix.GetForGuildAsync(Global.RedisClient, e.Guild.Id)
			).ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PrefixAsync(EventContext e)
		{
			if (string.IsNullOrEmpty(e.Arguments.ToString()))
			{
				e.ErrorEmbed(e.Locale.GetString("miki_module_general_prefix_error_no_arg")).ToEmbed().QueueToChannel(e.Channel);
				return;
			}

			PrefixInstance defaultInstance = e.commandHandler.GetDefaultPrefix();

			using (var context = new MikiContext())
			{
				await defaultInstance.ChangeForGuildAsync(context, Global.RedisClient, e.Guild.Id, e.Arguments.ToString());
			}

			EmbedBuilder embed = new EmbedBuilder();
			embed.SetTitle(e.Locale.GetString("miki_module_general_prefix_success_header"));
			embed.SetDescription(
				e.Locale.GetString("miki_module_general_prefix_success_message", e.Arguments.ToString()
			));

			embed.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command(Name = "syncavatar")]
		public async Task SyncAvatarAsync(EventContext e)
		{
			await Utils.SyncAvatarAsync(e.Author);

			e.SuccessEmbed(
				e.Locale.GetString("setting_avatar_updated")	
			).QueueToChannel(e.Channel);
		}

		[Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public Task ListLocaleAsync(EventContext e)
		{
			new EmbedBuilder()
			{
				Title = e.Locale.GetString("locales_available"),
				Description = ("`" + string.Join("`, `", Locale.LocaleNames.Keys) + "`")
			}.AddField(
				"Your language not here?",
				e.Locale.GetString("locales_contribute",
					$"[{e.Locale.GetString("locales_tranlations")}](https://poeditor.com/join/project/FIv7NBIReD)"
				)
			).ToEmbed().QueueToChannel(e.Channel);
			return Task.CompletedTask;
		}
	}
}