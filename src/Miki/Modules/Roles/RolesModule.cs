namespace Miki.Modules.Roles
{
    using Microsoft.EntityFrameworkCore;
    using Miki.Attributes;
    using Miki.Bot.Models;
    using Miki.Bot.Models.Exceptions;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Dsl;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Helpers;
    using Miki.Modules.Roles.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Miki.Localization;
    using Miki.Utility;

    [Module("Role Management")]
	internal class RolesModule
	{
        #region commands

        [GuildOnly, Command("iam")]
        public async Task IAmAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            string roleName = e.GetArgumentPack().Pack.TakeAll();

            List<IDiscordRole> roles = await GetRolesByName(e.GetGuild(), roleName);
            IDiscordRole role;

            // checking if the role has a duplicate name.
            if (roles.Count > 1)
            {
                List<LevelRole> levelRoles = await context.LevelRoles.Where(x => x.GuildId == (long)e.GetGuild().Id).ToListAsync();

                if (levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower()).Count() > 1)
                {
                    await e.ErrorEmbed("two roles configured have the same name.")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
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

            // checking if the role is null
            if (role == null)
            {
                await e.ErrorEmbedResource("error_role_null")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);

            if (author.RoleIds.Contains(role.Id))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_role_already_given"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            LevelRole newRole = await context.LevelRoles.FindAsync(e.GetGuild().Id.ToDbLong(), role.Id.ToDbLong());
            IDiscordRole discordRole = await newRole.GetRoleAsync();

            User user = (await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong()));

            IDiscordGuildUser discordUser = await e.GetGuild().GetMemberAsync(user.Id.FromDbLong());
            var localUser = await LocalExperience.GetAsync(
                context,
                e.GetGuild().Id,
                discordUser.Id);
            if (localUser == null)
            {
                localUser = await LocalExperience.CreateAsync(
                    context,
                    e.GetGuild().Id,
                    discordUser.Id,
                    discordUser.Username);
            }

            if (!newRole?.Optable ?? false)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_role_forbidden"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            int level = User.CalculateLevel(localUser.Experience);

            if (newRole.RequiredLevel > level)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_role_level_low", newRole.RequiredLevel - level))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            if (newRole.RequiredRole != 0 && !discordUser.RoleIds.Contains(newRole.RequiredRole.FromDbLong()))
            {
                var requiredRole = await e.GetGuild().GetRoleAsync(newRole.RequiredRole.FromDbLong());
                throw new RequiredRoleMissingException(requiredRole);
            }

            if (newRole.Price > 0)
            {
                user.RemoveCurrency(newRole.Price);
                await context.SaveChangesAsync();
            }

            var me = await e.GetGuild().GetSelfAsync();
            if (!await me.HasPermissionsAsync(GuildPermission.ManageRoles))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("permission_missing", "give roles")).ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            int hierarchy = await me.GetHierarchyAsync();

            if (discordRole.Position >= hierarchy)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "give roles")).ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            await author.AddRoleAsync(discordRole);

            await new EmbedBuilder()
                .SetTitle("I AM")
                .SetColor(128, 255, 128)
                .SetDescription($"You're a(n) {role.Name} now!")
                .ToEmbed()
                .QueueAsync(e, e.GetChannel());
        }

        [GuildOnly, Command("iamnot")]
        public async Task IAmNotAsync(IContext e)
        {
            string roleName = e.GetArgumentPack().Pack.TakeAll();

            var context = e.GetService<MikiDbContext>();

            List<IDiscordRole> roles = await GetRolesByName(e.GetGuild(), roleName);
            IDiscordRole role;

            if(roles.Count > 1)
            {
                List<LevelRole> levelRoles = await context.LevelRoles
                    .Where(x => x.GuildId == (long)e.GetGuild().Id).ToListAsync();
                if(levelRoles.Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower())
                       .Count() > 1)
                {
                    await e.ErrorEmbed("two roles configured have the same name.")
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }
                else
                {
                    role = levelRoles
                        .Where(x => x.GetRoleAsync().Result.Name.ToLower() == roleName.ToLower())
                        .FirstOrDefault().GetRoleAsync().Result;
                }
            }
            else
            {
                role = roles.FirstOrDefault();
            }

            if(role == null)
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_role_null"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);
            IDiscordGuildUser me = await e.GetGuild().GetSelfAsync();

            if(!author.RoleIds.Contains(role.Id))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("error_role_forbidden"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            LevelRole newRole =
                await context.LevelRoles.FindAsync(e.GetGuild().Id.ToDbLong(), role.Id.ToDbLong());
            User user = await context.Users.FindAsync(e.GetAuthor().Id.ToDbLong());

            if(!await me.HasPermissionsAsync(GuildPermission.ManageRoles))
            {
                await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "give roles"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            if(role.Position >= await me.GetHierarchyAsync())
            {
                await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "give roles"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            await author.RemoveRoleAsync(role);

            await new EmbedBuilder()
                .SetTitle("I AM NOT")
                .SetColor(255, 128, 128)
                .SetDescription($"You're no longer a(n) {role.Name}!")
                .ToEmbed().QueueAsync(e, e.GetChannel());
        }

        [GuildOnly, Command("iamlist")]
		public async Task IAmListAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();
            var locale = e.GetLocale();

            e.GetArgumentPack().Take(out int index);
                
                int page = Math.Max(index - 1, 0);
             
				long guildId = e.GetGuild().Id.ToDbLong();

				List<LevelRole> roles = await context.LevelRoles
					.Where(x => x.GuildId == guildId)
					.OrderBy(x => x.RoleId)
					.Skip(page * 25)
					.Take(25)
					.ToListAsync();

				StringBuilder stringBuilder = new StringBuilder();

				var guildRoles = (await e.GetGuild().GetRolesAsync()).ToList();

				var availableRoles = roles.Where(x => guildRoles.Any(y => x.RoleId == (long)y.Id))
					.Select(x => new Tuple<IDiscordRole, LevelRole>(
                        guildRoles.Single(y => x.RoleId == (long)y.Id), x))
                    .ToList();

				foreach (var role in availableRoles)
				{
					if (role.Item2.Optable)
					{
						if (role.Item1 == null)
						{
							context.LevelRoles.Remove(role.Item2);
							continue;
						}

						stringBuilder.Append($"`{role.Item1.Name.PadRight(20)}|`");

						if (role.Item2.RequiredLevel > 0)
						{
							stringBuilder.Append($"⭐{role.Item2.RequiredLevel} ");
						}

						if (role.Item2.Automatic)
						{
							stringBuilder.Append($"⚙️");
						}

						if (role.Item2.RequiredRole != 0)
                        {
                            var roleRequired = guildRoles.FirstOrDefault(
                                x => x.Id == (ulong)role.Item2.RequiredRole);

							stringBuilder.Append($"🔨`{roleRequired?.Name ?? "non-existing role"}`");
						}

						if (role.Item2.Price != 0)
						{
							stringBuilder.Append($"🔸  {role.Item2.Price} ");
						}

						stringBuilder.AppendLine();
					}
				}

				if (stringBuilder.Length == 0)
				{
					stringBuilder.Append(e.GetLocale().GetString("miki_placeholder_null"));
				}

				await context.SaveChangesAsync();

                await new EmbedBuilder().SetTitle("📄 Available Roles")
					.SetDescription(stringBuilder.ToString())
					.SetColor(204, 214, 221)
					.SetFooter("page " + (page + 1))
					.ToEmbed().QueueAsync(e, e.GetChannel());
		}

        [GuildOnly, Command("configrole")]
        public async Task ConfigRoleAsync(IContext e)
        {
            if (e.GetArgumentPack().CanTake)
            {
                await ConfigRoleQuickAsync(e);
            }
        }

        #endregion commands

        /*public async Task ConfigRoleInteractiveAsync(EventContext e)
		{
			using (var context = new MikiContext())
			{
				EmbedBuilder sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Type out the role name you want to config")
					.SetColor(138, 182, 239);
				IDiscordMessage sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());
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

				List<IDiscordRole> rolesFound = await GetRolesByName(e.GetGuild(), roleName.ToLower());
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

					role = await e.GetGuild().CreateRoleAsync(new CreateRoleArgs
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

					sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());
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

				LevelRole newRole = await context.LevelRoles.FindAsync(e.GetGuild().Id.ToDbLong(), role.Id.ToDbLong());
				if(newRole == null)
				{
					newRole = (await context.LevelRoles.AddAsync(new LevelRole()
					{
						RoleId = role.Id.ToDbLong(),
						GuildId = e.GetGuild().Id.ToDbLong()
					})).Entity;
				}

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription("Is there a role that is needed to get this role? Type out the role name, or `-` to skip")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());

				while (true)
				{
					msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

					rolesFound = (await GetRolesByName(e.GetGuild(), msg.Content.ToLower()));
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

						sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());
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

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());

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

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());

				msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());
				if (msg == null)
				{
					return;
				}

				newRole.Automatic = msg.Content.ToLower()[0] == 'y';

				sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
					.SetDescription($"Should users be able to opt in? type `yes`, otherwise it will be considered as no")
					.SetColor(138, 182, 239);

				sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());

				msg = await e.EventSystem.GetCommandHandler<MessageListener>().WaitForNextMessage(e.CreateSession());

				newRole.Optable = msg.Content.ToLower()[0] == 'y';

				if (newRole.Optable)
				{
					sourceEmbed = Utils.Embed.SetTitle("⚙ Interactive Mode")
						.SetDescription($"Do you want the user to pay mekos for the role? Enter any amount. Enter 0 to keep the role free.")
						.SetColor(138, 182, 239);

					sourceMessage = await sourceEmbed.ToEmbed().SendToChannel(e.GetChannel());

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
					.ToEmbed().QueueToChannelAsync(e.GetChannel());
			}
		}*/

        public async Task ConfigRoleQuickAsync(IContext e)
        {
            var context = e.GetService<MikiDbContext>();

            if (!e.GetArgumentPack().Take(out string roleName))
            {
                await e.ErrorEmbed("Expected a role name")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            IDiscordRole role = null;
            if (ulong.TryParse(roleName, out ulong s))
            {
                role = await e.GetGuild().GetRoleAsync(s);
            }
            else
            {
                role = (await GetRolesByName(e.GetGuild(), roleName)).FirstOrDefault();
            }

            LevelRole newRole = await context.LevelRoles.FindAsync(e.GetGuild().Id.ToDbLong(), role.Id.ToDbLong());
            MSLResponse arguments = new MMLParser(e.GetArgumentPack().Pack.TakeAll())
                .Parse();

            if (role.Name.Length > 20)
            {
                await e.ErrorEmbed("Please keep role names below 20 letters.")
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }

            if (newRole == null)
            {
                newRole = context.LevelRoles.Add(new LevelRole()
                {
                    GuildId = (e.GetGuild().Id.ToDbLong()),
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

            if (arguments.HasKey("price"))
            {
                newRole.Price = arguments.GetInt("price");
                if (newRole.Price < 0)
                {
                    throw new ArgumentLessThanZeroException();
                }
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
                    var r = (await e.GetGuild().GetRolesAsync())
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

            await new EmbedBuilder()
                .SetTitle("⚙ Role Config")
                .SetColor(102, 117, 127)
                .SetDescription($"Updated {role.Name}!")
                .ToEmbed().QueueAsync(e, e.GetChannel());
        }

        public async Task<List<IDiscordRole>> GetRolesByName(IDiscordGuild guild, string roleName)
        {
            var roles = await guild.GetRolesAsync();
			return roles
                .Where(x => string.Equals(
                    x.Name, roleName, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
		} 
	}
}