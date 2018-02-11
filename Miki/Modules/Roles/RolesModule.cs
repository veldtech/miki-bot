using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common.Events;
using Miki.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Miki.Dsl;
using Miki.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Modules.Roles
{
	[Module(Name = "Role Management")]
	class RolesModule
	{
		#region commands
		[Command(Name = "iam")]
		public async Task IAmAsync(EventContext e)
		{
			IDiscordRole role = GetRoleByName(e.Guild, e.arguments);

			if(role == null)
			{
				await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_null"))
					.SendToChannel(e.Channel);
				return;
			}

			if (e.Author.RoleIds.Contains(role.Id))
			{
				await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_already_given"))
					.SendToChannel(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				IDiscordUser discordUser = await e.Guild.GetUserAsync(user.Id.FromDbLong());

				if (newRole == null || !newRole.Optable)
				{
					await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_forbidden"))
						.SendToChannel(e.Channel);
					return;
				}

				if(newRole.RequiredLevel > user.Level)
				{
					await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_level_low", newRole.RequiredLevel - user.Level))
						.SendToChannel(e.Channel);
					return;
				}

				if(newRole.RequiredRole != 0 && !discordUser.RoleIds.Contains(newRole.RequiredRole.FromDbLong()))
				{
					await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_required", $"**{e.Guild.GetRole(newRole.RequiredRole.FromDbLong()).Name}**"))
						.SendToChannel(e.Channel);
					return;
				}

				if (newRole.Price > 0)
				{
					if (user.Currency >= newRole.Price)
					{
						await e.Channel.SendMessageAsync($"Getting this role costs you {newRole.Price} mekos! type `yes` to proceed.");
						IDiscordMessage m = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
						if (m.Content.ToLower()[0] == 'y')
						{

							User serverOwner = await context.Users.FindAsync(e.Guild.OwnerId.ToDbLong());
							await user.AddCurrencyAsync(-newRole.Price);
							await serverOwner.AddCurrencyAsync(newRole.Price);
							await context.SaveChangesAsync();

						}
					}
					else
					{
						await e.ErrorEmbed(e.Channel.GetLocale().GetString("user_error_insufficient_mekos"))
							.SendToChannel(e.Channel);
						return;
					}
				}

				await e.Author.AddRoleAsync(newRole.Role);

				Utils.Embed.SetTitle("I AM")
					.SetColor(128, 255, 128)
					.SetDescription($"You're a(n) {role.Name} now!")
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamnot")]
		public async Task IAmNotAsync(EventContext e)
		{
			IDiscordRole role = GetRoleByName(e.Guild, e.arguments);

			if (role == null)
			{
				await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_null"))
					.SendToChannel(e.Channel);
				return;
			}

			if (!e.Author.RoleIds.Contains(role.Id))
			{
				await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_forbidden"))
					.SendToChannel(e.Channel);
				return;
			}

			using (var context = new MikiContext())
			{
				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				await e.Author.RemoveRoleAsync(newRole.Role);

				Utils.Embed.SetTitle("I AM NOT")
					.SetColor(255, 128, 128)
					.SetDescription($"You're no longer a(n) {role.Name}!")
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamlist")]
		public async Task IAmListAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				long guildId = e.Guild.Id.ToDbLong();
				List<LevelRole> roles = await context.LevelRoles
					.Where(x => x.GuildId == guildId)
					.OrderBy(x => x.RoleId)
					.Skip(0)
					.Take(25)
					.ToListAsync();

				StringBuilder stringBuilder = new StringBuilder();

				foreach(var role in roles)
				{
					if(role.Optable)
					{
						if(role.Role == null)
						{
							context.LevelRoles.Remove(role);
						}
						stringBuilder.Append($"`{role.Role.Name.PadRight(20)}|`");
						if(role.RequiredLevel > 0)
						{
							stringBuilder.Append($"⭐{role.RequiredLevel} ");
						}
						if(role.Automatic)
						{
							stringBuilder.Append($"⚙️");
						}
						if (role.RequiredRole != 0)
						{
							stringBuilder.Append($"🔨`{e.Guild.GetRole(role.RequiredRole.FromDbLong())?.Name ?? "non-existing role"}`");
						}
						if (role.Price != 0)
						{
							stringBuilder.Append($"🔸{role.Price} ");
						}
						stringBuilder.AppendLine();
					}
				}

				if(stringBuilder.Length == 0)
				{
					stringBuilder.Append(e.Channel.GetLocale().GetString("miki_placeholder_null"));
				}

				await context.SaveChangesAsync();
					
				Utils.Embed.SetTitle("Available Roles")
					.SetDescription(stringBuilder.ToString())
					.QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "configrole", Accessibility = Miki.Common.EventAccessibility.ADMINONLY)]
		public async Task ConfigRoleAsync(EventContext e)
		{
			if(string.IsNullOrWhiteSpace(e.arguments))
			{
				Task.Run(async () => await ConfigRoleInteractiveAsync(e));
			}
			else
			{
				await ConfigRoleQuickAsync(e);
			}
		}
		#endregion

		public async Task ConfigRoleInteractiveAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				IDiscordEmbed sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Type out the role name you want to config")
					.SetColor(138, 182, 239);
				IDiscordMessage sourceMessage = await sourceEmbed.SendToChannel(e.Channel);
				IDiscordMessage msg = null;

				while (true)
				{
					msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
	
					if (msg.Content.Length < 20)
					{
						break;
					}

					sourceMessage.Modify("", e.ErrorEmbed("That role name is way too long! Try again."));
				}

				IDiscordRole role = GetRoleByName(e.Guild, msg.Content.ToLower());
				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				if(newRole == null)
				{
					newRole = (await context.LevelRoles.AddAsync(new LevelRole()
					{
						RoleId = role.Id.ToDbLong(),
						GuildId = e.Guild.Id.ToDbLong()
					})).Entity;
				}

				await e.Channel.SendMessageAsync($"Is there a role requirement? type a role name in, if there is no requirement type -");

				while (true)
				{
					msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
					if (msg == null)
					{
						return;
					}

					IDiscordRole parentRole = GetRoleByName(e.Guild, msg.Content.ToLower());

					if (parentRole != null || msg.Content == "-")
					{
						newRole.RequiredRole = (long?)parentRole?.Id ?? 0;
						break;
					}
					await e.Channel.SendMessageAsync($"Couldn't find that role, sorry. Try again.");
				}

				await e.Channel.SendMessageAsync($"Is there a level requirement? type a number, if there is no requirement type 0");

				while (true)
				{
					msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
					if (msg == null)
					{
						return;
					}

					if (int.TryParse(msg.Content, out int r))
					{
						if (r >= 0)
						{
							newRole.RequiredLevel = r;
							break;
						}
						else
						{
							await e.Channel.SendMessageAsync($"Please pick a number above 0. Try again");
						}
					}
					else
					{
						await e.Channel.SendMessageAsync($"Are you sure `{msg.Content}` is a number? Try again");
					}
				}

				if(newRole.RequiredRole > 0)
				{
					await e.Channel.SendMessageAsync($"Should I give them when the user level ups? type yes, otherwise it will be considered as no");
					msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
					if (msg == null)
					{
						return;
					}

					newRole.Automatic = msg.Content.ToLower()[0] == 'y';
				}

				await e.Channel.SendMessageAsync($"Should users be able to opt in? type yes, otherwise it will be considered as no");
				msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
				if (msg == null)
				{
					return;
				}

				newRole.Optable = msg.Content.ToLower()[0] == 'y';

				if (newRole.Optable)
				{
					while (true)
					{
						await e.Channel.SendMessageAsync($"Do you want the user to pay mekos for the role? Enter any amount. Enter 0 to keep the role free.");
						msg = await EventSystem.Instance.ListenForNextMessageAsync(e.Channel.Id, e.Author.Id);
						if (msg == null)
						{
							return;
						}

						if (int.TryParse(msg.Content, out int r))
						{
							if (r >= 0)
							{
								newRole.Price = r;
								break;
							}
							else
							{
								await e.Channel.SendMessageAsync($"Please pick a number above 0. Try again");
							}
						}
						else
						{
							await e.Channel.SendMessageAsync($"Are you sure `{msg.Content}` is a number? Try again");
						}
					}
				}

				await context.SaveChangesAsync();
				Utils.Embed.SetTitle("⚙ Role Config")
					.SetColor(102, 117, 127)
					.SetDescription($"Updated {role.Name}!")
					.QueueToChannel(e.Channel);
			}
		}

		public async Task ConfigRoleQuickAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				MSLResponse arguments = new MMLParser(e.arguments)
					.Parse();

				IDiscordRole role = GetRoleByName(e.Guild, e.arguments.Split('"')[1]);
				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());

				if (role.Name.Length > 20)
				{
					await e.ErrorEmbed("Please keep role names below 20 letters.")
						.SendToChannel(e.Channel);
					return;
				}

				if (newRole == null)
				{
					newRole = context.LevelRoles.Add(new LevelRole()
					{
						GuildId = (e.Guild.Id.ToDbLong()),
						RoleId = (role.Id.ToDbLong()),
					}).Entity;
				}

				if (arguments.HasKey("automatic"))
				{
					newRole.Automatic = arguments.GetBool("automatic");
				}

				if (arguments.HasKey("optable"))
				{
					newRole.Optable = arguments.GetBool("optable");
				}

				if (arguments.HasKey("level-required"))
				{
					newRole.RequiredLevel = arguments.GetInt("level-required");
				}

				if (arguments.HasKey("role-required"))
				{
					long id = 0;
					if (arguments.TryGet("role-required", out long l))
					{
						id = l;
					}
					else
					{
						var r = e.Guild.Roles.Where(x => x.Name.ToLower() == arguments.GetString("role-required").ToLower()).FirstOrDefault();
						if (r != null)
						{
							id = r.Id.ToDbLong();
						}
					}

					if (id != 0)
					{
						newRole.RequiredRole = id;
					}
				}

				await context.SaveChangesAsync();
				Utils.Embed.SetTitle("⚙ Role Config")
					.SetColor(102, 117, 127)
					.SetDescription($"Updated {role.Name}!")
					.QueueToChannel(e.Channel);
			}

		}

		public IDiscordRole GetRoleByName(IDiscordGuild guild, string roleName)
		{
			IDiscordRole role = guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).FirstOrDefault();
			return role;
		}
	}
}
