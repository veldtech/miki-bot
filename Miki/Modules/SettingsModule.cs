using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Languages;
using Miki.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Miki.Dsl;
using System;
using Discord;
using Miki.Framework.Extension;

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
		// TODO: turn into fancy generic function
		[Command(Name = "setnotifications", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetupNotifications(EventContext e)
		{
			if (string.IsNullOrWhiteSpace(e.Arguments.ToString()))
				Task.Run(async () => await SetupNotificationsInteractive<LevelNotificationsSetting>(e, DatabaseSettingId.LEVEL_NOTIFICATIONS));
			else
			{
				MMLParser mml = new MMLParser(e.Arguments.ToString());
				MSLResponse response = mml.Parse();

				bool global = response.GetBool("g");
				LevelNotificationsSetting type = Enum.Parse<LevelNotificationsSetting>(response.GetString("type"));

				await Setting.UpdateAsync(e.Channel.Id, DatabaseSettingId.LEVEL_NOTIFICATIONS, (int)type);
			}
		}

		public async Task SetupNotificationsInteractive<T>(EventContext e, DatabaseSettingId settingId)
		{
			List<string> options = Enum.GetNames(typeof(T))
				.Select(x => x.ToLower()
					.Replace('_', ' '))
				.ToList();

			string settingName = settingId.ToString().ToLower().Replace('_', ' ');

			var sEmbed= SettingsBaseEmbed;
			sEmbed.Description = ($"What kind of {settingName} do you want");
			sEmbed.AddInlineField("Options", string.Join("\n", options));
			var sMsg = await sEmbed.Build().SendToChannel(e.Channel);

			int newSetting;

			while (true)
			{
				var msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

				if (Enum.TryParse<LevelNotificationsSetting>(msg.Content.Replace(" ", "_"), true, out var setting))
				{
					newSetting = (int)setting;
					break;
				}

				await sMsg.ModifyAsync(x =>
				{
					x.Embed = e.ErrorEmbed("Oh, that didn't seem right! Try again")
						.AddInlineField("Options", string.Join("\n", options))
						.Build();
				});
			}

			sMsg = await SettingsBaseEmbed
				.WithDescription("Do you want this to apply for every channel? say `yes` if you do.")
				.Build().SendToChannel(e.Channel);

			var cMsg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
			bool global = (cMsg.Content.ToLower()[0] == 'y');

			await SettingsBaseEmbed
				.WithDescription($"Setting `{settingName}` Updated!")
				.Build().SendToChannel(e.Channel);

			if (!global)
			{
				await Setting.UpdateAsync(e.Channel.Id, settingId, newSetting);
			}
			else
			{
				await Setting.UpdateGuildAsync(e.Guild, settingId, newSetting);
			}
		}

		[Command(Name = "showmodule")]
		public async Task ConfigAsync(EventContext e)
		{
			Module module = e.commandHandler.GetModule(e.Arguments.ToString());

			if (module != null)
			{
				EmbedBuilder embed = new EmbedBuilder();

				embed.Title = (e.Arguments.ToString().ToUpper());

				string content = "";

				foreach (CommandEvent ev in module.Events.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				embed.AddInlineField("Events", content);

				content = "";

				foreach (BaseService ev in module.Services.OrderBy((x) => x.Name))
				{
					content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
				}

				if (!string.IsNullOrEmpty(content))
					embed.AddInlineField("Services", content);

				embed.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "showmodules")]
		public async Task ShowModulesAsync(EventContext e)
		{
			List<string> modules = new List<string>();
			CommandHandler commandHandler = e.EventSystem.CommandHandler;
			EventAccessibility userEventAccessibility = commandHandler.GetUserAccessibility(e.message);

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
				string output = $"{(await e.commandHandler.GetModule(modules[i]).IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>")} {modules[i]}\n";
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
			{
				Title = ($"Module Status for '{e.Channel.Name}'")
			}.AddInlineField("Column 1", firstColumn)
			.AddInlineField("Column 2", secondColumn)
			.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetLocale(EventContext e)
		{
			string localeName = e.Arguments.FirstOrDefault()?.Argument ?? "";

			if (Locale.LocaleNames.TryGetValue(localeName, out string langId))
			{
				await Locale.SetLanguageAsync(e.Channel.Id.ToDbLong(), langId);
				Utils.SuccessEmbed(e.Channel.GetLocale(), e.GetResource("localization_set", $"`{localeName}`"))
					.QueueToChannel(e.Channel);
				return;
			}
			e.ErrorEmbed($"{localeName} is not a valid language. use `>listlocale` to check all languages available.")
				.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task PrefixAsync(EventContext e)
		{
			Locale locale = new Locale(e.Channel.Id);

			if (string.IsNullOrEmpty(e.Arguments.ToString()))
			{
				e.ErrorEmbed(locale.GetString("miki_module_general_prefix_error_no_arg")).Build().QueueToChannel(e.Channel);
				return;
			}

			await PrefixInstance.Default.ChangeForGuildAsync(e.Guild.Id, e.Arguments.ToString());

			EmbedBuilder embed = Utils.Embed;
			embed.Title = locale.GetString("miki_module_general_prefix_success_header");
			embed.Description = locale.GetString("miki_module_general_prefix_success_message", e.Arguments.ToString());

			embed.AddField(locale.GetString("miki_module_general_prefix_example_command_header"), $"{e.Arguments.ToString()}profile");

			embed.Build().QueueToChannel(e.Channel);
		}

		[Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public void DoListLocale(EventContext e)
		{
			new EmbedBuilder() {
				Title = ("Available locales"),
				Description = ("`" + string.Join("`, `", Locale.LocaleNames.Keys) + "`")
			}.Build().QueueToChannel(e.Channel);
		}

		private EmbedBuilder SettingsBaseEmbed =>
			new EmbedBuilder()
			{
				Title = ("⚙ Settings"),
				Color = new Color(138, 182, 239)
			};
	}
}