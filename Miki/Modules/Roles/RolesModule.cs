using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
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
			using (var context = new MikiContext())
			{
				List<IDiscordRole> roles = GetRolesByName(e.Guild, e.arguments);
				IDiscordRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();
					if(levelRoles.Where(x => x.Role.Name.ToLower() == e.arguments.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.Role.Name.ToLower() == e.arguments.ToLower()).FirstOrDefault().Role;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

				if (role == null)
				{
					e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_null"))
						.QueueToChannel(e.Channel);
					return;
				}

				if (e.Author.RoleIds.Contains(role.Id))
				{
					e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_already_given"))
						.QueueToChannel(e.Channel);
					return;
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());
				IDiscordUser discordUser = await e.Guild.GetUserAsync(user.Id.FromDbLong());

				if (!newRole?.Optable ?? false)
				{
					await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_forbidden"))
						.SendToChannel(e.Channel);
					return;
				}

				if (newRole.RequiredLevel > user.Level)
				{
					await e.ErrorEmbed(e.Channel.GetLocale().GetString("error_role_level_low", newRole.RequiredLevel - user.Level))
						.SendToChannel(e.Channel);
					return;
				}

				if (newRole.RequiredRole != 0 && !discordUser.RoleIds.Contains(newRole.RequiredRole.FromDbLong()))
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
						IDiscordMessage m = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
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
			using (var context = new MikiContext())
			{
				List<IDiscordRole> roles = GetRolesByName(e.Guild, e.arguments);
				IDiscordRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();
					if (levelRoles.Where(x => x.Role.Name.ToLower() == e.arguments.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.Role.Name.ToLower() == e.arguments.ToLower()).FirstOrDefault().Role;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

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
					.Skip(0)
					.Take(25)
					.ToListAsync();

				StringBuilder stringBuilder = new StringBuilder();

				roles = roles.OrderBy(x => x.Role.Name).ToList();

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
					
				Utils.Embed.SetTitle("📄 Available Roles")
					.SetDescription(stringBuilder.ToString())
					.SetColor(204, 214, 221)
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
					msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
	
					if (msg.Content.Length < 20)
					{
						break;
					}

					sourceMessage.Modify("", e.ErrorEmbed("That role name is way too long! Try again."));
				}

				List<IDiscordRole> rolesFound = GetRolesByName(e.Guild, msg.Content.ToLower());
				IDiscordRole role = null;

				if(rolesFound.Count > 1)
				{
					string roleIds = string.Join("\n", rolesFound.Select(x => $"`{x.Name}`: {x.Id}"));

					if (roleIds.Length > 1024)
					{
						roleIds = roleIds.Substring(0, 1024);
					}

					sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
							.SetDescription("I found multiple roles with that name, which one would you like? please enter the ID")
							.AddInlineField("Roles - Ids", roleIds)
							.SetColor(138, 182, 239);

					sourceMessage = await sourceEmbed.SendToChannel(e.Channel);
					while(true)
					{
						msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
						if(ulong.TryParse(msg.Content, out ulong id))
						{
							role = rolesFound.Where(x => x.Id == id)
								.FirstOrDefault();

							if(role != null)
							{
								break;
							}
							else
							{
								sourceMessage.Modify(null, e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)));
							}
						}
						else
						{
							sourceMessage.Modify(null, e.ErrorEmbed("I couldn't find that role. Try again!")
								.AddInlineField("Roles - Ids", string.Join("\n", roleIds)));
						}
					}
				}
				else
				{
					role = rolesFound.FirstOrDefault();
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				if(newRole == null)
				{
					newRole = (await context.LevelRoles.AddAsync(new LevelRole()
					{
						RoleId = role.Id.ToDbLong(),
						GuildId = e.Guild.Id.ToDbLong()
					})).Entity;
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Is there a role that is needed to get this role? Type out the role name, or `-` to skip")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.SendToChannel(e.Channel);

				while (true)
				{
					msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

					rolesFound = GetRolesByName(e.Guild, msg.Content.ToLower());
					IDiscordRole parentRole = null;

					if (rolesFound.Count > 1)
					{
						string roleIds = string.Join("\n", rolesFound.Select(x => $"`{x.Name}`: {x.Id}"));

						if (roleIds.Length > 1024)
						{
							roleIds = roleIds.Substring(0, 1024);
						}

						sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
								.SetDescription("I found multiple roles with that name, which one would you like? please enter the ID")
								.AddInlineField("Roles - Ids", roleIds)
								.SetColor(138, 182, 239);

						sourceMessage = await sourceEmbed.SendToChannel(e.Channel);
						while (true)
						{
							msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
							if (ulong.TryParse(msg.Content, out ulong id))
							{
								parentRole = rolesFound.Where(x => x.Id == id)
									.FirstOrDefault();

								if (parentRole != null)
								{
									break;
								}
								else
								{
									sourceMessage.Modify(null, e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
										.AddInlineField("Roles - Ids", string.Join("\n", roleIds)));
								}
							}
							else
							{
								sourceMessage.Modify(null, e.ErrorEmbed("I couldn't find that role. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)));
							}
						}
					}
					else
					{
						parentRole = rolesFound.FirstOrDefault();
					}

					if (parentRole != null || msg.Content == "-")
					{
						newRole.RequiredRole = (long?)parentRole?.Id ?? 0;
						break;
					}

					sourceMessage.Modify(null, e.ErrorEmbed("I couldn't find that role. Try again!"));
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Is there a level requirement? type a number, if there is no requirement type 0")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.SendToChannel(e.Channel);

				while (true)
				{
					msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

					if (int.TryParse(msg.Content, out int r))
					{
						if (r >= 0)
						{
							newRole.RequiredLevel = r;
							break;
						}
						else
						{
							sourceMessage.Modify(null, sourceEmbed.SetDescription($"Please pick a number above 0. Try again"));
						}
					}
					else
					{
						sourceMessage.Modify(null, sourceEmbed.SetDescription($"Are you sure `{msg.Content}` is a number? Try again"));
					}
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Should I give them when the user level ups? type `yes`, otherwise it will be considered as no")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.SendToChannel(e.Channel);
					
				msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
				if (msg == null)
				{
					return;
				}

				newRole.Automatic = msg.Content.ToLower()[0] == 'y';

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Should users be able to opt in? type `yes`, otherwise it will be considered as no")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.SendToChannel(e.Channel);

				msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

				newRole.Optable = msg.Content.ToLower()[0] == 'y';

				if (newRole.Optable)
				{
					sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
						.SetDescription($"Do you want the user to pay mekos for the role? Enter any amount. Enter 0 to keep the role free.")
						.SetColor(138, 182, 239);

					sourceMessage = await sourceEmbed.SendToChannel(e.Channel);

					while (true)
					{
						msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
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
								sourceMessage.Modify(null, e.ErrorEmbed($"Please pick a number above 0. Try again"));
							}
						}
						else
						{
							sourceMessage.Modify(null, e.ErrorEmbed($"Not quite sure if this is a number 🤔"));
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
				string roleName = e.arguments.Split('"')[1];

				IDiscordRole role = null;
				if (ulong.TryParse(roleName, out ulong s))
				{
					role = e.Guild.GetRole(s);
				}
				else
				{
					role = GetRolesByName(e.Guild, roleName).FirstOrDefault();
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());

				MSLResponse arguments = new MMLParser(e.arguments.Substring(roleName.Length + 3))
					.Parse();

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

		public List<IDiscordRole> GetRolesByName(IDiscordGuild guild, string roleName)
		=> guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).ToList();
		
	}
}
