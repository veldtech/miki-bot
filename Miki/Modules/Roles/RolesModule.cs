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
using Miki.Framework.Extension;
using Miki.Exceptions;
using Miki.Framework.Events.Commands;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Helpers;

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

				List<IDiscordRole> roles = await GetRolesByName(e.Guild, roleName);
				IDiscordRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();

					if (levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.ToEmbed().QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower()).FirstOrDefault().GetRoleAsync().Result;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

				if (role == null)
				{
					e.ErrorEmbedResource("error_role_null")
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				IDiscordGuildUser author = await e.Guild.GetMemberAsync(e.Author.Id);

				if (author.RoleIds.Contains(role.Id))
				{
					e.ErrorEmbed(e.Locale.GetString("error_role_already_given"))
						.ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = (await context.Users.FindAsync(e.Author.Id.ToDbLong()));

				IDiscordGuildUser discordUser = await e.Guild.GetMemberAsync(user.Id.FromDbLong());
				LocalExperience localUser = await LocalExperience.GetAsync(context, e.Guild.Id.ToDbLong(), discordUser.Id.ToDbLong(), discordUser.Username);

				if (!newRole?.Optable ?? false)
				{
					await e.ErrorEmbed(e.Locale.GetString("error_role_forbidden"))
						.ToEmbed().SendToChannel(e.Channel);
					return;
				}

				int level = User.CalculateLevel(localUser.Experience);

				if (newRole.RequiredLevel > level)
				{
					await e.ErrorEmbed(e.Locale.GetString("error_role_level_low", newRole.RequiredLevel - level))
						.ToEmbed().SendToChannel(e.Channel);
					return;
				}

				if (newRole.RequiredRole != 0 && !discordUser.RoleIds.Contains(newRole.RequiredRole.FromDbLong()))
				{
					var requiredRole = await e.Guild.GetRoleAsync(newRole.RequiredRole.FromDbLong());

					e.ErrorEmbed(
						e.Locale.GetString(
							"error_role_required", $"**{requiredRole.Name}**"
						)
					).ToEmbed().QueueToChannel(e.Channel);
					return;
				}

				if (newRole.Price > 0)
				{
					if (user.Currency >= newRole.Price)
					{
						await e.Channel.SendMessageAsync($"Getting this role costs you {newRole.Price} mekos! type `yes` to proceed.");
						IDiscordMessage m = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
						if (m.Content.ToLower()[0] == 'y')
						{
							await user.AddCurrencyAsync(-newRole.Price);
							await context.SaveChangesAsync();
						}
						else
						{
							await e.ErrorEmbed("Purchase Cancelled")
								.ToEmbed().SendToChannel(e.Channel);
							return;
						}
					}
					else
					{
						await e.ErrorEmbed(e.Locale.GetString("user_error_insufficient_mekos"))
							.ToEmbed().SendToChannel(e.Channel);
						return;
					}
				}

				var me = await e.Guild.GetSelfAsync();

				if(!await me.HasPermissionsAsync(GuildPermission.ManageRoles))
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "give roles")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				if (newRole.GetRoleAsync().Result.Position >= await me.GetHierarchyAsync())
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "give roles")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				await author.AddRoleAsync(newRole.GetRoleAsync().Result);

				Utils.Embed.SetTitle("I AM")
					.SetColor(128, 255, 128)
					.SetDescription($"You're a(n) {role.Name} now!")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamnot")]
		public async Task IAmNotAsync(EventContext e)
		{
			string roleName = e.Arguments.ToString();

			using (var context = new MikiContext())
			{
				List<IDiscordRole> roles = await GetRolesByName(e.Guild, roleName);
				IDiscordRole role = null;

				if (roles.Count > 1)
				{
					List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.Guild.Id).ToListAsync();
					if (levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower()).Count() > 1)
					{
						e.ErrorEmbed("two roles configured have the same name.")
							.ToEmbed().QueueToChannel(e.Channel);
						return;
					}
					else
					{
						role = levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower()).FirstOrDefault().GetRoleAsync().Result;
					}
				}
				else
				{
					role = roles.FirstOrDefault();
				}

				if (role == null)
				{
					await e.ErrorEmbed(e.Locale.GetString("error_role_null"))
						.ToEmbed().SendToChannel(e.Channel);
					return;
				}

				IDiscordGuildUser author = await e.Guild.GetMemberAsync(e.Author.Id);
				IDiscordGuildUser me = await e.Guild.GetSelfAsync();

				if (!author.RoleIds.Contains(role.Id))
				{
					await e.ErrorEmbed(e.Locale.GetString("error_role_forbidden"))
						.ToEmbed().SendToChannel(e.Channel);
					return;
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());
				User user = await context.Users.FindAsync(e.Author.Id.ToDbLong());

				if (!await me.HasPermissionsAsync(GuildPermission.ManageRoles))
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "give roles")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}

				if ((await newRole.GetRoleAsync()).Position >= await me.GetHierarchyAsync())
				{
					e.ErrorEmbed(e.Locale.GetString("permission_error_low", "give roles")).ToEmbed()
						.QueueToChannel(e.Channel);
					return;
				}


				await author.RemoveRoleAsync(newRole.GetRoleAsync().Result);

				Utils.Embed.SetTitle("I AM NOT")
					.SetColor(255, 128, 128)
					.SetDescription($"You're no longer a(n) {role.Name}!")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		[Command(Name = "iamlist")]
		public async Task IAmListAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				int page = Math.Max((e.Arguments.Join()?.AsInt() ?? 0) - 1, 0);

				long guildId = e.Guild.Id.ToDbLong();

				// TODO: consider adding a name of the role in the database.
				List<LevelRole> roles = await context.LevelRoles
					.Where(x => x.GuildId == guildId)
					.OrderBy(x => x.RoleId)
					.Skip(page * 25)
					.Take(25)
					.ToListAsync();

				StringBuilder stringBuilder = new StringBuilder();

				roles = roles.OrderBy(x => x.GetRoleAsync().Result?.Name ?? "").ToList();

				foreach(var role in roles)
				{
					if(role.Optable)
					{
						if(role.GetRoleAsync().Result == null)
						{
							context.LevelRoles.Remove(role);
							continue;
						}

						stringBuilder.Append($"`{role.GetRoleAsync().Result.Name.PadRight(20)}|`");

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
							var roleRequired = await e.Guild.GetRoleAsync(role.RequiredRole.FromDbLong());

							stringBuilder.Append($"🔨`{roleRequired?.Name ?? "non-existing role"}`");
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
					stringBuilder.Append(e.Locale.GetString("miki_placeholder_null"));
				}

				await context.SaveChangesAsync();
					
				Utils.Embed.SetTitle("📄 Available Roles")
					.SetDescription(stringBuilder.ToString())
					.SetColor(204, 214, 221)
					.SetFooter("page " + (page + 1))
					.ToEmbed().QueueToChannel(e.Channel);
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
				EmbedBuilder sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Type out the role name you want to config")
					.SetColor(138, 182, 239);
				IDiscordMessage sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);
				IDiscordMessage msg = null;

				while (true)
				{
					msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

					if (msg.Content.Length < 20)
					{
						break;
					}
					else
					{
						await sourceMessage.EditAsync(new EditMessageArgs
						{
							embed = e.ErrorEmbed("That role name is way too long! Try again.")
								.ToEmbed()
						});
					}
				}

				string roleName = msg.Content;	

				List<IDiscordRole> rolesFound = await GetRolesByName(e.Guild, roleName.ToLower());
				IDiscordRole role = null;

				if(rolesFound.Count == 0)
				{
					// Hey, I couldn't find this role, Can I make a new one?
					await sourceMessage.EditAsync(new EditMessageArgs
					{
						embed = e.ErrorEmbed($"There's no role that is named `{roleName}`, Shall I create it? Y/N").ToEmbed()
					});

					msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

					if (msg.Content.ToLower()[0] != 'y')
					{
						throw new RoleNullException();
					}

					role = await e.Guild.CreateRoleAsync(new CreateRoleArgs
					{
						Name = roleName,
					});
				}
				else if (rolesFound.Count > 1)
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

					sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);
					while(true)
					{
						msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
						if (ulong.TryParse(msg.Content, out ulong id))
						{
							role = rolesFound.Where(x => x.Id == id)
								.FirstOrDefault();

							if (role != null)
							{
								break;
							}
							else
							{
								await sourceMessage.EditAsync(new EditMessageArgs {
									embed = e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).ToEmbed()
								});
							}
						}
						else
						{
							await sourceMessage.EditAsync(new EditMessageArgs
							{
								embed = e.ErrorEmbed("I couldn't find that role. Try again!")
								.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).ToEmbed()
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

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Is there a role that is needed to get this role? Type out the role name, or `-` to skip")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);

				while (true)
				{
					msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

					rolesFound = (await GetRolesByName(e.Guild, msg.Content.ToLower()));
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

						sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);
						while (true)
						{
							msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
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
									await sourceMessage.EditAsync(new EditMessageArgs {
										embed = e.ErrorEmbed("I couldn't find that role id in the list. Try again!")
										.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).ToEmbed()
									}) ;
								}
							}
							else
							{
								await sourceMessage.EditAsync(new EditMessageArgs
								{
									embed = e.ErrorEmbed("I couldn't find that role. Try again!")
									.AddInlineField("Roles - Ids", string.Join("\n", roleIds)).ToEmbed()
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

					await sourceMessage.EditAsync(new EditMessageArgs
					{
						embed = e.ErrorEmbed("I couldn't find that role. Try again!").ToEmbed()
					});
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Is there a level requirement? type a number, if there is no requirement type 0")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);

				while (true)
				{
					msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

					if (int.TryParse(msg.Content, out int r))
					{
						if (r >= 0)
						{
							newRole.RequiredLevel = r;
							break;
						}
						else
						{
							await sourceMessage.EditAsync(new EditMessageArgs
							{
								embed = sourceEmbed.SetDescription($"Please pick a number above 0. Try again")
									.ToEmbed()
							});
						}
					}
					else
					{
						await sourceMessage.EditAsync(new EditMessageArgs
						{
							embed = sourceEmbed.SetDescription($"Are you sure `{msg.Content}` is a number? Try again").ToEmbed()
						});
					}
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Should I give them when the user level ups? type `yes`, otherwise it will be considered as no")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);

				msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
				if (msg == null)
				{
					return;
				}

				newRole.Automatic = msg.Content.ToLower()[0] == 'y';

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Should users be able to opt in? type `yes`, otherwise it will be considered as no")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);

				msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

				newRole.Optable = msg.Content.ToLower()[0] == 'y';

				if (newRole.Optable)
				{
					sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
						.SetDescription($"Do you want the user to pay mekos for the role? Enter any amount. Enter 0 to keep the role free.")
						.SetColor(138, 182, 239);

					sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.Channel);

					while (true)
					{
						msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

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
								await sourceMessage.EditAsync(new EditMessageArgs
								{
									embed = e.ErrorEmbed($"Please pick a number above 0. Try again").ToEmbed()
								});
							}
						}
						else
						{
							await sourceMessage.EditAsync(new EditMessageArgs
							{
								embed = e.ErrorEmbed($"Not quite sure if this is a number 🤔").ToEmbed()
							});
						}
					}
				}

				await context.SaveChangesAsync();
				Utils.Embed.SetTitle("⚙ Role Config")
					.SetColor(102, 117, 127)
					.SetDescription($"Updated {role.Name}!")
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		public async Task ConfigRoleQuickAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				string roleName = e.Arguments.ToString().Split('"')[1];

				IDiscordRole role = null;
				if (ulong.TryParse(roleName, out ulong s))
				{
					role = await e.Guild.GetRoleAsync(s);
				}
				else
				{
					role = (await GetRolesByName(e.Guild, roleName)).FirstOrDefault();
				}

				LevelRole newRole = await context.LevelRoles.FindAsync(e.Guild.Id.ToDbLong(), role.Id.ToDbLong());

				MSLResponse arguments = new MMLParser(e.Arguments.ToString().Substring(roleName.Length + 3))
					.Parse();

				if (role.Name.Length > 20)
				{
					await e.ErrorEmbed("Please keep role names below 20 letters.")
						.ToEmbed().SendToChannel(e.Channel);
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
						var r = (await e.Guild.GetRolesAsync())
							.Where(x => x.Name.ToLower() == arguments.GetString("role-required").ToLower())
							.FirstOrDefault();

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
					.ToEmbed().QueueToChannel(e.Channel);
			}
		}

		public async Task<List<IDiscordRole>> GetRolesByName(IDiscordGuild guild, string roleName)
			=> (await guild.GetRolesAsync()).Where(x => x.Name.ToLower() == roleName.ToLower()).ToList();
		
	}
}
