namespace Miki.Modules.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Miki.Bot.Models;
    using Miki.Discord;
    using Miki.Discord.Common;
    using Miki.Discord.Rest;
    using Miki.Framework;
    using Miki.Framework.Commands;
    using Miki.Framework.Commands.Permissions;
    using Miki.Framework.Commands.Permissions.Attributes;
    using Miki.Framework.Commands.Permissions.Exceptions;
    using Miki.Framework.Commands.Permissions.Models;
    using Miki.Framework.Commands.Scopes.Attributes;
    using Miki.Localization;
    using Miki.Modules.Admin.Exceptions;
    using Miki.Utility;

    [Module("Admin")]
    public class AdminModule
    {
        #region resource uris
        private const string PruneErrorNoMessages = "miki_module_admin_prune_no_messages";
        private const string PruneSuccess = "miki_module_admin_prune_success";
        #endregion

        [Command("ban")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task BanAsync(IContext e)
        {
            IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
            if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser))
                .HasFlag(GuildPermission.BanMembers))
            {
                e.GetArgumentPack().Take(out string userName);
                if (userName == null)
                {
                    return;
                }

                var user = await e.GetGuild().FindUserAsync(userName);

                IDiscordGuildUser author = await e.GetGuild()
                    .GetMemberAsync(e.GetAuthor().Id);

                if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "ban")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "ban")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                int prune = 1;
                if (e.GetArgumentPack().Take(out int pruneDays))
                {
                    prune = pruneDays;
                }

                string reason = e.GetArgumentPack().Pack.TakeAll();

                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = "🛑 BAN",
                    Description = e.GetLocale().GetString("ban_header", $"**{e.GetGuild().Name}**")
                };

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField($"💬 {e.GetLocale().GetString("miki_module_admin_kick_reason")}", reason);
                }

                embed.AddInlineField(
                    $"💁 {e.GetLocale().GetString("miki_module_admin_kick_by")}",
                    $"{e.GetAuthor().Username}#{e.GetAuthor().Discriminator}");

                await embed.ToEmbed().SendToUser(user);

                await e.GetGuild().AddBanAsync(user, prune, reason);
            }
            else
            {
                await e.ErrorEmbed(e.GetLocale().GetString("permission_needed_error", $"`{e.GetLocale().GetString("permission_ban_members")}`"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }

        [Command("clean")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task CleanAsync(IContext e)
        {
            await PruneAsync(e, (await e.GetGuild().GetSelfAsync()).Id, null);
        }

        [Command("kick")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task KickAsync(IContext e)
        {
            IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
            var locale = e.GetLocale();

            if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.KickMembers))
            {
                IDiscordGuildUser bannedUser;
                IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);

                e.GetArgumentPack().Take(out string userName);

                bannedUser = await e.GetGuild().FindUserAsync(userName);

                if (await bannedUser.GetHierarchyAsync() >= await author.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "kick")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                if (await bannedUser.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "kick")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                string reason = "";
                if (e.GetArgumentPack().CanTake)
                {
                    reason = e.GetArgumentPack().Pack.TakeAll();
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = locale.GetString("miki_module_admin_kick_header"),
                    Description = locale.GetString(
                        "miki_module_admin_kick_description", 
                        e.GetGuild().Name)
                };

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddField(locale.GetString("miki_module_admin_kick_reason"), reason, true);
                }

                embed.AddField(
                    locale.GetString("miki_module_admin_kick_by"),
                    $"{author.Username}#{author.Discriminator}", 
                    true);

                embed.Color = new Color(1, 1, 0);

                await embed.ToEmbed()
                    .SendToUser(bannedUser);
                await bannedUser.KickAsync(reason);
            }
            else
            {
                await e.ErrorEmbed(
                        e.GetLocale().GetString("permission_needed_error", $"`{e.GetLocale().GetString("permission_kick_members")}`"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }

        [Command("prune")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task PruneAsync(IContext e)
        {
            await PruneAsync(e, 0, null);
        }

        public async Task PruneAsync(IContext e, ulong target = 0, string filter = null)
        {
            IDiscordGuildUser invoker = await e.GetGuild()
                .GetSelfAsync();
            var locale = e.GetLocale();

            if (!(await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(invoker)).HasFlag(GuildPermission.ManageMessages))
            {
                e.GetChannel()
                    .QueueMessage(e, null, locale.GetString("miki_module_admin_prune_error_no_access"));
                return;
            }

            if (e.GetArgumentPack().Pack.Length < 1)
            {
                await new EmbedBuilder()
                    .SetTitle("♻ Prune")
                    .SetColor(119, 178, 85)
                    .SetDescription(locale.GetString("miki_module_admin_prune_no_arg"))
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
                return;
            }


            string args = e.GetArgumentPack().Pack.TakeAll();
            string[] argsSplit = args.Split(' ');
            target = e.GetMessage().MentionedUserIds.Count > 0
                ? (await e.GetGuild().GetMemberAsync(e.GetMessage().MentionedUserIds.First())).Id
                : target;

            if (int.TryParse(argsSplit[0], out int amount))
            {
                if (amount < 0)
                {
                    await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_negative"))
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }
                if (amount > 100)
                {
                    await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_max"))
                        .ToEmbed().QueueAsync(e, e.GetChannel());
                    return;
                }
            }
            else
            {
                await Utils.ErrorEmbed(e, locale.GetString("miki_module_admin_prune_error_parse"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            if (Regex.IsMatch(e.GetArgumentPack().Pack.TakeAll(), "\"(.*?)\""))
            {
                Regex regex = new Regex("\"(.*?)\"");
                filter = regex.Match(e.GetArgumentPack().Pack.TakeAll()).ToString().Trim('"', ' ');
            }

            await e.GetMessage()
                .DeleteAsync(); // Delete the calling message before we get the message history.

            List<IDiscordMessage> messages = (await e.GetChannel().GetMessagesAsync(amount)).ToList();
            List<IDiscordMessage> deleteMessages = new List<IDiscordMessage>();

            amount = messages.Count();

            if (amount < 1)
            {
                await e.GetMessage().DeleteAsync();

                await e.ErrorEmbed(locale.GetString(PruneErrorNoMessages, e.GetPrefixMatch()))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }
            for (int i = 0; i < amount; i++)
            {
                if(target != 0 && messages[i]?.Author.Id != target)
                {
                    continue;
                }

                if(filter != null && messages[i]?.Content.IndexOf(filter) < 0)
                {
                    continue;
                }


                if (messages.ElementAt(i).Timestamp.AddDays(14) > DateTime.Now)
                {
                    deleteMessages.Add(messages.ElementAt(i));
                }
            }

            if (deleteMessages.Count > 0)
            {
                await e.GetChannel()
                    .DeleteMessagesAsync(deleteMessages.ToArray());
            }

            await e.SuccessEmbedResource(PruneSuccess, deleteMessages.Count)
                .QueueAsync(e, 
                    e.GetChannel(), 
                    modifier: x => x.ThenWait(5000).ThenDelete());
        }

        [Command("permissions", "perms")]
        public class PermissionsCommand
        {
            private const string PermissionSet = "permission_set";

            private const string DisabledEmoji = "<:icon_disabled:627870695799652362>";
            private const string EnabledEmoji = "<:icon_enabled:627870695807778821>";
            private const string DefaultEmoji = "<:icon_default:627870695493337089>";

            [Command("allow")]
            [DefaultPermission(PermissionStatus.Deny)]
            public Task AllowPermissionsAsync(IContext e)
            {
                return SetPermissionsAsync(e, PermissionStatus.Allow);
            }

            [Command("deny")]
            [DefaultPermission(PermissionStatus.Deny)]
            public Task DenyPermissionsAsync(IContext e)
            {
                return SetPermissionsAsync(e, PermissionStatus.Deny);
            }

            [Command("reset")]
            [DefaultPermission(PermissionStatus.Deny)]
            public Task ResetPermissionsAsync(IContext e)
            {
                return SetPermissionsAsync(e, PermissionStatus.Default);
            }

            [Command("check")]
            [RequiresScope("developer")]
            public Task CheckPermissionsAsync(IContext e)
            {
                return e.GetGuild()
                    .FindUserAsync(e)
                    .Map(x =>
                    {
                        var ids = new List<long>();
                        ids.AddRange(x.RoleIds.Select(x => (long)x));
                        ids.Add((long)x.Id);
                        ids.Add((long)e.GetChannel().Id);
                        ids.Add((long)e.GetGuild().Id);
                        return ids;
                    })
                    .Map(x =>
                    {
                        var service = e.GetService<PermissionService>();
                        e.GetArgumentPack().Take(out string command);
                        return service.GetPriorityPermissionAsync(
                                (long)e.GetGuild().Id, command, x.ToArray())
                            .AsTask();
                    })
                    .Map(permission => e.GetChannel()
                        .SendMessageAsync(permission?.ToString() ?? "none"));
            }

            [Command("list")]
            [DefaultPermission(PermissionStatus.Allow)]
            public async Task ListPermissionsAsync(IContext e)
            {
               var permissions = e.GetService<PermissionService>();

                List<long> idList = new List<long>();
                if(e.GetAuthor() is IDiscordGuildUser gm)
                {
                    idList.AddRange(gm.RoleIds.Select(x => (long)x));
                }
                idList.Add((long)e.GetGuild().Id);
                idList.Add((long)e.GetChannel().Id);
                idList.Add((long)e.GetAuthor().Id);

                var allPermissions = await permissions.ListPermissionsAsync(
                    (long)e.GetGuild().Id, idList.ToArray());
                if (e.GetArgumentPack().Take(out string commandName))
                {
                    var commandTree = e.GetService<CommandTree>();
                    var command = commandTree.GetCommand(commandName);
                    if (command != null)
                    {
                        allPermissions = allPermissions
                            .Where(x => x.CommandName == command.ToString())
                            .ToList();
                    }
                }

                if(!allPermissions.Any())
                {
                    await e.GetChannel()
                        .SendMessageAsync("empty");
                    return;
                }

                allPermissions = allPermissions.Where(x => x.Status != PermissionStatus.Default)
                    .OrderBy(x => x.CommandName)
                    .ThenBy(x => x.Status)
                    .ToList();

                StringBuilder description = new StringBuilder();

                foreach(var p in allPermissions)
                {
                    description.Append($"{GetStatusEmoji(p.Status)} {p.CommandName} for {p.Type} ");
                    description.Append(await GetEntityName(e, p));
                    description.Append("\n");
                }

                await new EmbedBuilder()
                    .SetTitle("⚡ Your permissions")
                    .SetColor(180, 180, 90)
                    .SetDescription(description.ToString())
                    .ToEmbed()
                    .QueueAsync(e, e.GetChannel());
            }

            private async Task<string> GetEntityName(IContext context, Permission p)
            {
                return p.Type switch
                {
                    EntityType.User     => context.GetAuthor().Username,
                    EntityType.Channel  => (await context.GetGuild().GetChannelAsync((ulong)p.EntityId)).Name,
                    EntityType.Role     => (await context.GetGuild().GetRoleAsync((ulong)p.EntityId)).Name,
                    EntityType.Guild    => context.GetGuild().Name,
                    EntityType.Global   => "",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            private string GetStatusEmoji(PermissionStatus status)
            {
                switch (status)
                {
                    case PermissionStatus.Allow:
                        return EnabledEmoji;
                    case PermissionStatus.Default:
                        return DefaultEmoji;
                    case PermissionStatus.Deny:
                        return DisabledEmoji;
                    default:
                        return "";
                }
            }

            private class Entity
            {
                public long Id { get; set; }
                public string Resource { get; set; }
                public EntityType Type { get; set; }
            }

            private async Task SetPermissionsAsync(IContext e, PermissionStatus level)
            {
                var permissions = e.GetService<PermissionService>();
                var commands = e.GetService<CommandTree>();

                if(!e.GetArgumentPack().Take(out string commandName))
                {
                    return;
                }
                var command = commands.GetCommand(commandName.Replace('.', ' '));
                if(!(command is IExecutable))
                {
                    throw new InvalidEntityException("command");
                }

                var ownPermission = await permissions.GetPriorityPermissionAsync(e);
                if(ownPermission.Status == PermissionStatus.Deny)
                {
                    throw new PermissionUnauthorizedException("");
                }

                Entity entity = await GetEntityAsync(e);

                await permissions.SetPermissionAsync(new Permission
                {
                    EntityId = entity.Id,
                    Type = entity.Type,
                    CommandName = command.ToString(),
                    Status = level,
                    GuildId = (long)e.GetGuild().Id
                });

                await e.SuccessEmbedResource(PermissionSet, entity.Resource, level)
                    .QueueAsync(e, e.GetChannel());
            }

            private async ValueTask<Entity> GetEntityAsync(IContext e)
            {
                if(!e.GetArgumentPack().Take(out string type))
                {
                    return null;
                }

                if(Enum.TryParse<EntityType>(type, true, out var entityType))
                {
                    return await GetEntityFromType(e, entityType);
                }

                if(Mention.TryParse(type, out var mention))
                {
                    return await GetEntityFromMention(e, mention);
                }
                return null;
            }

            private Task<Entity> GetEntityFromType(IContext e, EntityType type)
                => type switch
                {
                    EntityType.User => e.GetGuild()
                        .FindUserAsync(e)
                        .Map(x => new Entity
                        {
                            Id = (long)x.Id,
                            Resource = x.Username,
                            Type = EntityType.User
                        }),

                    EntityType.Channel => e.GetGuild()
                        .FindChannelAsync(e)
                        .Map(x => new Entity
                        {
                            Id = (long)x.Id,
                            Resource = x.Name,
                            Type = EntityType.Channel
                        }),

                    EntityType.Role => e.GetGuild()
                        .FindRoleAsync(e)
                        .Map(x => new Entity
                        {
                            Id = (long)x.Id,
                            Resource = x.Name,
                            Type = EntityType.Role
                        }),

                    EntityType.Guild => Task.FromResult(new Entity
                    {
                        Id = (long)e.GetGuild().Id,
                        Resource = e.GetGuild().Name,
                        Type = EntityType.Guild
                    }),

                    _ => throw new NotSupportedException(),
                };

            private async ValueTask<Entity> GetEntityFromMention(IContext e, Mention mention)
            {
                var entity = new Entity
                {
                    Id = (long) mention.Id
                };

                switch (mention.Type)
                {
                    case MentionType.USER:
                    case MentionType.USER_NICKNAME:
                    {
                        var user = await e.GetGuild().GetMemberAsync(mention.Id);
                        if (user == null)
                        {
                            throw new InvalidEntityException("member");
                        }

                        entity.Resource = user.Username;
                        entity.Type = EntityType.User;
                    } break;

                    case MentionType.CHANNEL:
                    {
                        var channel = await e.GetGuild().GetChannelAsync(mention.Id);
                        if(channel == null)
                        {
                            throw new InvalidEntityException("channel");
                        }

                        entity.Resource = channel.Name;
                        entity.Type = EntityType.User;
                    } break;

                    case MentionType.ROLE:
                    {
                        var role = await e.GetGuild().GetRoleAsync(mention.Id);
                        if(role == null)
                        {
                            throw new InvalidEntityException("role");
                        }

                        entity.Resource = role.Name;
                        entity.Type = EntityType.User;
                    } break;

                    default:
                        throw new NotSupportedException();
                }
                return entity;
            }
        }

        [Command("setevent", "setcommand")]
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task SetCommandAsync(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string commandId))
            {
                // require command argument
                return;
            }

            commandId = commandId.Replace('.', ' ');

            var handler = e.GetService<CommandTree>();

            var command = handler.GetCommand(commandId);
            if (command == null)
            {
                await e.ErrorEmbed($"'{commandId}' is not a valid command")
                    .ToEmbed().QueueAsync(e, e.GetChannel());
                return;
            }

            if (!e.GetArgumentPack().Take(out bool setValue))
            {
                return;
            }

            string localeState = (setValue)
                ? e.GetLocale().GetString("miki_generic_enabled")
                : e.GetLocale().GetString("miki_generic_disabled");

            bool global = false;

            var context = e.GetService<MikiDbContext>();

            if (e.GetArgumentPack().Peek(out string g))
            {
                if (g == "-g")
                {
                    global = true;
                    var channels = await e.GetGuild().GetChannelsAsync();
                    foreach (var c in channels)
                    {
                        // TODO: implement in a better way.
                    }
                }
            }

            await context.SaveChangesAsync();

            string outputDesc = localeState + " " + commandId;
            if (global)
            {
                outputDesc += " in every channel";
            }

            await e.SuccessEmbed(outputDesc)
                .QueueAsync(e, e.GetChannel());
        }

        [Command("softban")] // softban ulong, softban string, so
        [DefaultPermission(PermissionStatus.Deny)]
        public async Task SoftbanAsync(IContext e)
        {
            IDiscordGuildUser currentUser = await e.GetGuild().GetSelfAsync();
            if ((await (e.GetChannel() as IDiscordGuildChannel).GetPermissionsAsync(currentUser)).HasFlag(GuildPermission.BanMembers))
            {
                if (!e.GetArgumentPack().Take(out string argObject))
                {
                    return;
                }

                IDiscordGuildUser user = await e.GetGuild().FindUserAsync(argObject);
               
                string reason = null;
                if (e.GetArgumentPack().CanTake)
                {
                    reason = e.GetArgumentPack()
                        .Pack.TakeAll();
                }

                IDiscordGuildUser author = await e.GetGuild().GetMemberAsync(e.GetAuthor().Id);

                if (await user.GetHierarchyAsync() >= await author.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "softban")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                if (await user.GetHierarchyAsync() >= await currentUser.GetHierarchyAsync())
                {
                    await e.ErrorEmbed(e.GetLocale().GetString("permission_error_low", "softban")).ToEmbed()
                        .QueueAsync(e, e.GetChannel());
                    return;
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = "⚠ SOFTBAN",
                    Description = $"You've been banned from **{e.GetGuild().Name}**!"
                };

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    embed.AddInlineField("💬 Reason", reason);
                }

                embed.AddInlineField(
                    "💁 Banned by",
                    $"{author.Username}#{author.Discriminator}");

                await embed.ToEmbed()
                    .SendToUser(user);

                await e.GetGuild().AddBanAsync(user, 1, reason);
                await e.GetGuild().RemoveBanAsync(user);
            }
            else
            {
                await e.ErrorEmbed(e.GetLocale().GetString(
                    "permission_needed_error",
                    $"`{e.GetLocale().GetString("permission_ban_members")}`"))
                    .ToEmbed().QueueAsync(e, e.GetChannel());
            }
        }
    }
}
