using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Miki.Languages;
using Miki.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Miki.Modules
{
    [Module(Name = "settings")]
    internal class SettingsModule
    {
        [Command(Name = "toggledm")]
        public async Task ToggleDmAsync(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseSettingId.PERSONALMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting()
					{
						EntityId = e.Author.Id.ToDbLong(),
						IsEnabled = true,
						SettingId = DatabaseSettingId.PERSONALMESSAGE
					}).Entity;
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

                setting.IsEnabled = !setting.IsEnabled;
                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_dm", aa);
                embed.Color = (setting.IsEnabled) ? new Miki.Common.Color(1, 0, 0) : new Miki.Common.Color(0, 1, 0);

                await context.SaveChangesAsync();
                embed.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "toggleerrors")]
        public async Task ToggleErrors(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Author.Id.ToDbLong(), DatabaseSettingId.ERRORMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting() { EntityId = e.Author.Id.ToDbLong(), IsEnabled = true, SettingId = DatabaseSettingId.ERRORMESSAGE }).Entity;
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_error_dm", aa);
                embed.Color = (setting.IsEnabled) ? new Miki.Common.Color(1, 0, 0) : new Miki.Common.Color(0, 1, 0);

                await context.SaveChangesAsync();
                embed.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "toggleguildnotifications", Aliases = new string[] { "tgn" }, Accessibility = EventAccessibility.ADMINONLY)]
        public async Task ToggleGuildNotifications(EventContext e)
        {
            using (var context = new MikiContext())
            {
                Setting setting = await context.Settings.FindAsync(e.Guild.Id.ToDbLong(), DatabaseSettingId.CHANNELMESSAGE);

                if (setting == null)
                {
                    setting = context.Settings.Add(new Setting()
					{
						EntityId = e.Guild.Id.ToDbLong(),
						IsEnabled = true,
						SettingId = DatabaseSettingId.CHANNELMESSAGE
					}).Entity;
                }

                IDiscordEmbed embed = Utils.Embed;
                Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());
                setting.IsEnabled = !setting.IsEnabled;

                string aa = (!setting.IsEnabled) ? locale.GetString("miki_generic_disabled") : locale.GetString("miki_generic_enabled");

                embed.Description = locale.GetString("miki_module_settings_guild_notifications", aa);
                embed.Color = (setting.IsEnabled) ? new Miki.Common.Color(1, 0, 0) : new Miki.Common.Color(0, 1, 0);

                await context.SaveChangesAsync();
                embed.QueueToChannel(e.Channel);
            }
        }

        [Command(Name = "showmodule")]
        public async Task ConfigAsync(EventContext e)
        {
            IModule module = e.commandHandler.GetModule(e.arguments);

            if (module != null)
            {
                IDiscordEmbed embed = Utils.Embed.SetTitle( e.arguments.ToUpper() );

                string content = "";

                foreach (RuntimeCommandEvent ev in module.Events.OrderBy((x) => x.Name))
                {
                    content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
                }

                embed.AddInlineField("Events", content);

                content = "";

                foreach (IService ev in module.Services.OrderBy((x) => x.Name))
                {
					content += (await ev.IsEnabled(e.Channel.Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
                }

				if (!string.IsNullOrEmpty(content))
					embed.AddInlineField("Services", content);

                embed.QueueToChannel(e.Channel);
            }
        }

		[Command(Name = "showmodules")]
		public async Task ShowModulesAsync( EventContext e )
		{
			List<string> modules = new List<string>();
			CommandHandler commandHandler = Bot.instance.Events.CommandHandler;
			EventAccessibility userEventAccessibility = commandHandler.GetUserAccessibility( e.message );

			foreach( ICommandEvent ev in commandHandler.Commands.Values )
			{
				if( userEventAccessibility >= ev.Accessibility )
				{
					if( ev.Module != null && !modules.Contains( ev.Module.Name.ToUpper() ) )
					{
						modules.Add( ev.Module.Name.ToUpper() );
					}
				}
			}

			modules.Sort();

			string firstColumn = "", secondColumn = "";

			for(int i = 0; i < modules.Count(); i++)
			{
				string output = $"{( await e.commandHandler.GetModule( modules[i] ).IsEnabled( e.Channel.Id ) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>" )} {modules[i]}\n";
				if(i < modules.Count() / 2 + 1)
				{
					firstColumn += output; 
				} 
				else 
				{
					secondColumn += output;
				}
			}

			Utils.Embed.SetTitle( $"Module Status for '{e.Channel.Name}'" )
				.AddInlineField( "Column 1", firstColumn )
				.AddInlineField( "Column 2", secondColumn )
				.QueueToChannel( e.Channel );
		}

		[Command(Name = "setlocale", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task SetLocale(EventContext e)
		{
			if (Locale.LocaleNames.TryGetValue(e.arguments.ToLower(), out string langId))
			{
				await Locale.SetLanguageAsync(e.Channel.Id.ToDbLong(), langId);
				Utils.SuccessEmbed(e.Channel.GetLocale(), e.GetResource("localization_set", $"`{e.arguments}`"))
					.QueueToChannel(e.Channel);
				return;
			}
			e.ErrorEmbed( $"{e.arguments} is not a valid language. use `>listlocale` to check all languages available.")
				.QueueToChannel(e.Channel);
		}

        [Command(Name = "setprefix", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task PrefixAsync(EventContext e)
        {
            Locale locale = Locale.GetEntity(e.Channel.Id.ToDbLong());

            if (string.IsNullOrEmpty(e.arguments))
            {
                e.ErrorEmbed(locale.GetString("miki_module_general_prefix_error_no_arg")).QueueToChannel(e.Channel);
                return;
            }

            await PrefixInstance.Default.ChangeForGuildAsync(e.Guild.Id, e.arguments);

            IDiscordEmbed embed = Utils.Embed;
            embed.Title = locale.GetString("miki_module_general_prefix_success_header");
            embed.Description = locale.GetString("miki_module_general_prefix_success_message", e.arguments);

            embed.AddField(locale.GetString("miki_module_general_prefix_example_command_header"), $"{e.arguments}profile");

            embed.QueueToChannel(e.Channel);
        }

        [Command(Name = "listlocale", Accessibility = EventAccessibility.ADMINONLY)]
        public async Task DoListLocale(EventContext e)
        {
            Utils.Embed.SetTitle("Available locales")
                .SetDescription("`" + string.Join("`, `", Locale.LocaleNames.Keys) + "`")
                .QueueToChannel(e.Channel.Id);
        }
    }
}