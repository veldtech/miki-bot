using Miki.Framework;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Common;
using Microsoft.EntityFrameworkCore;
using Miki.Dsl;
using Miki.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Miki.Framework.Extension;
using Miki.Exceptions;

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
				string roleName = e.Arguments.ToString();

				List<IRole> roles = GetRolesByName(e.Guild, roleName);
				IRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();
					if(levelRoles.Where(x => x.Role.Name.ToLower() == roleName.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.Build().QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.Role.Name.ToLower() == roleName.ToLower()).FirstOrDefault().Role;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

				if (role == null)
				{
					e.ErrorEmbedResource("error_role_null")
						.Build().QueueToChannel(e.Channel);
					return;
				}

				if ((e.Author as IGuildUser).RoleIds.Contains(role.Id))
				{
					e.ErrorEmbed(e.GetResource("error_role_already_given"))
						.Build().QueueToChannel(e.Channel);
					return;
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = (await context.Users.FindAsync(e.Author.Id.ToDbLong()));
					
				IGuildUser discordUser = await e.Guild.GetUserAsync(user.Id.FromDbLong());
				LocalExperience localUser = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), discordUser);

				if (!newRole?.Optable ?? false)
				{
					await e.ErrorEmbed(e.GetResource("error_role_forbidden"))
						.Build().SendToChannel(e.Channel);
					return;
				}

				int level = User.CalculateLevel(localUser.Experience);

				if (newRole.RequiredLevel > level)
				{
					await e.ErrorEmbed(e.GetResource("error_role_level_low", newRole.RequiredLevel - level))
						.Build().SendToChannel(e.Channel);
					return;
				}

				if (newRole.RequiredRole != 0 && !discordUser.RoleIds.Contains(newRole.RequiredRole.FromDbLong()))
				{
					await e.ErrorEmbed(e.GetResource("error_role_required", $"**{e.Guild.GetRole(newRole.RequiredRole.FromDbLong()).Name}**"))
						.Build().SendToChannel(e.Channel);
					return;
				}

				if (newRole.Price > 0)
				{
					if (user.Currency >= newRole.Price)
					{
						await e.Channel.SendMessageAsync($"Getting this role costs you {newRole.Price} mekos! type `yes` to proceed.");
						IMessage m = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
						if (m.Content.ToLower()[0] == 'y')
						{
							await user.AddCurrencyAsync(-newRole.Price);
							await context.SaveChangesAsync();
						}
						else
						{
							await e.ErrorEmbed("Purchase Cancelled")
								.Build().SendToChannel(e.Channel);
							return;
						}
					}
					else
					{
						await e.ErrorEmbed(e.GetResource("user_error_insufficient_mekos"))
							.Build().SendToChannel(e.Channel);
						return;
					}
				}

				await (e.Author as IGuildUser).AddRoleAsync(newRole.Role);

				Utils.Embed.WithTitle("I AM")
					.WithColor(128, 255, 128)
					.WithDescription($"You're a(n) {role.Name} now!")
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamnot")]
		public async Task IAmNotAsync(EventContext e)
		{
			string roleName = e.Arguments.ToString();

			using (var context = new MikiContext())
			{
				List<IRole> roles = GetRolesByName(e.Guild, roleName);
				IRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();
					if (levelRoles.Where(x => x.Role.Name.ToLower() == roleName.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.Build().QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.Role.Name.ToLower() == roleName.ToLower()).FirstOrDefault().Role;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

				if (role == null)
				{
					await e.ErrorEmbed(e.GetResource("error_role_null"))
						.Build().SendToChannel(e.Channel);
					return;
				}

				if (!(e.Author as IGuildUser).RoleIds.Contains(role.Id))
				{
					await e.ErrorEmbed(e.GetResource("error_role_forbidden"))
						.Build().SendToChannel(e.Channel);
					return;
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				await (e.Author as IGuildUser).RemoveRoleAsync(newRole.Role);

				Utils.Embed.WithTitle("I AM NOT")
					.WithColor(255, 128, 128)
					.WithDescription($"You're no longer a(n) {role.Name}!")
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamlist")]
		public async Task IAmListAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				int page = Math.Max((e.Arguments.Join()?.AsInt(0) ?? 0) - 1, 0);

				long guildId = e.Guild.Id.ToDbLong();
				List<LevelRole> roles = await context.LevelRoles
					.Where(x => x.GuildId == guildId)
					.Skip(page * 25)
					.Take(25)
					.ToListAsync();

				StringBuilder stringBuilder = new StringBuilder();

				roles = roles.OrderBy(x => x.Role?.Name ?? "").ToList();

				foreach(var role in roles)
				{
					if(role.Optable)
					{
						if(role.Role == null)
						{
							context.LevelRoles.Remove(role);
							continue;
						}

						stringBuilder.Append($"`{role.Role.Name.PadRight(20)}|`");

						if (role.RequiredLevel > 0)
						{
							stringBuilder.Append($"⭐{role.RequiredLevel} ");
						}

						if (role.Automatic)
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
					stringBuilder.Append(e.GetResource("miki_placeholder_null"));
				}

				await context.SaveChangesAsync();
					
				Utils.Embed.WithTitle("📄 Available Roles")
					.WithDescription(stringBuilder.ToString())
					.WithColor(204, 214, 221)
					.WithFooter("page " + (page + 1))
					.Build().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "configrole", Accessibility = EventAccessibility.ADMINONLY)]
		public async Task ConfigRoleAsync(EventContext e)
		{
			if(string.IsNullOrWhiteSpace(e.Arguments.ToString()))
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
				EmbedBuilder sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
					.WithDescription("Type out the role name you want to config")
					.WithColor(138, 182, 239);
				IUserMessage sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);
				IMessage msg = null;

				while (true)
				{
					msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

					if (msg.Content.Length < 20)
					{
						break;
					}
					else
					{
						await sourceMessage.ModifyAsync(x =>
						{
							x.Embed = e.ErrorEmbed("That role name is way too long! Try again.").Build();
						});
					}
				}

				List<IRole> rolesFound = GetRolesByName(e.Guild, msg.Content.ToLower());
				IRole role = null;

				if(rolesFound.Count == 0)
				{
					throw new RoleNullException();
				}

				if(rolesFound.Count > 1)
				{
					string roleIds = string.Join("\n", rolesFound.Select(x => $"`{x.Name}`: {x.Id}"));

					if (roleIds.Length > 1024)
					{
						roleIds = roleIds.Substring(0, 1024);
					}

					sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
							.WithDescription("I found multiple roles with that name, which one would you like? please enter the ID")
							.AddInlineField("Roles - Ids", roleIds)
							.WithColor(138, 182, 239);

					sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);
					while(true)
					{
						msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
						if(ulong.TryParse(msg.Content, out ulong id))
						{
							role = rolesFound.Where(x => x.Id == id)
								.FirstOrDefault();

							if (role != null)
							{
								break;
							}
							else
							{
								await sourceMessage.ModifyAsync(x => {
									x.Embed = e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).Build();
								});
							}
						}
						else
						{
							await sourceMessage.ModifyAsync(x => {
								x.Embed = e.ErrorEmbed("I couldn't find that role. Try again!")
								.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).Build();
							});
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

				sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
					.WithDescription("Is there a role that is needed to get this role? Type out the role name, or `-` to skip")
					.WithColor(138, 182, 239);

				sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);

				while (true)
				{
					msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

					rolesFound = GetRolesByName(e.Guild, msg.Content.ToLower());
					IRole parentRole = null;

					if (rolesFound.Count > 1)
					{
						string roleIds = string.Join("\n", rolesFound.Select(x => $"`{x.Name}`: {x.Id}"));

						if (roleIds.Length > 1024)
						{
							roleIds = roleIds.Substring(0, 1024);
						}

						sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
								.WithDescription("I found multiple roles with that name, which one would you like? please enter the ID")
								.AddInlineField("Roles - Ids", roleIds)
								.WithColor(138, 182, 239);

						sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);
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
									await sourceMessage.ModifyAsync(x => {
										x.Embed = e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
										.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).Build();
									}) ;
								}
							}
							else
							{
								await sourceMessage.ModifyAsync(x =>
								{
									x.Embed = e.ErrorEmbed("I couldn't find that role. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).Build();
								});
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

					await sourceMessage.ModifyAsync(x =>
					{
						x.Embed = e.ErrorEmbed("I couldn't find that role. Try again!").Build();
					});
				}

				sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
					.WithDescription($"Is there a level requirement? type a number, if there is no requirement type 0")
					.WithColor(138, 182, 239);

				sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);

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
							await sourceMessage.ModifyAsync(x =>
							{
								x.Embed = sourceEmbed.WithDescription($"Please pick a number above 0. Try again").Build();
							});
						}
					}
					else
					{
						await sourceMessage.ModifyAsync(x =>
						{
							x.Embed = sourceEmbed.WithDescription($"Are you sure `{msg.Content}` is a number? Try again").Build();
						});
					}
				}

				sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
					.WithDescription($"Should I give them when the user level ups? type `yes`, otherwise it will be considered as no")
					.WithColor(138, 182, 239);

				sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);
					
				msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);
				if (msg == null)
				{
					return;
				}

				newRole.Automatic = msg.Content.ToLower()[0] == 'y';

				sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
					.WithDescription($"Should users be able to opt in? type `yes`, otherwise it will be considered as no")
					.WithColor(138, 182, 239);

				sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);

				msg = await EventSystem.Instance.ListenNextMessageAsync(e.Channel.Id, e.Author.Id);

				newRole.Optable = msg.Content.ToLower()[0] == 'y';

				if (newRole.Optable)
				{
					sourceEmbed = Utils.Embed.WithTitle("⚙ Interactive Mode")
						.WithDescription($"Do you want the user to pay mekos for the role? Enter any amount. Enter 0 to keep the role free.")
						.WithColor(138, 182, 239);

					sourceMessage = await sourceEmbed.Build().SendToChannel(e.Channel);

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
								await sourceMessage.ModifyAsync(x =>
								{
									x.Embed = e.ErrorEmbed($"Please pick a number above 0. Try again").Build();
								});
							}
						}
						else
						{
							await sourceMessage.ModifyAsync(x =>
							{
								x.Embed = e.ErrorEmbed($"Not quite sure if this is a number 🤔").Build();
							});
						}
					}
				}

				await context.SaveChangesAsync();
				Utils.Embed.WithTitle("⚙ Role Config")
					.WithColor(102, 117, 127)
					.WithDescription($"Updated {role.Name}!")
					.Build().QueueToChannel(e.Channel);
			}
		}

		public async Task ConfigRoleQuickAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				string roleName = e.Arguments.ToString().Split('"')[1];

				IRole role = null;
				if (ulong.TryParse(roleName, out ulong s))
				{
					role = e.Guild.GetRole(s);
				}
				else
				{
					role = GetRolesByName(e.Guild, roleName).FirstOrDefault();
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());

				MSLResponse arguments = new MMLParser(e.Arguments.ToString().Substring(roleName.Length + 3))
					.Parse();

				if (role.Name.Length > 20)
				{
					await e.ErrorEmbed("Please keep role names below 20 letters.")
						.Build().SendToChannel(e.Channel);
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
				Utils.Embed.WithTitle("⚙ Role Config")
					.WithColor(102, 117, 127)
					.WithDescription($"Updated {role.Name}!")
					.Build().QueueToChannel(e.Channel);
			}

		}

		public List<IRole> GetRolesByName(IGuild guild, string roleName)
		=> guild.Roles.Where(x => x.Name.ToLower() == roleName.ToLower()).ToList();
		
	}
}
