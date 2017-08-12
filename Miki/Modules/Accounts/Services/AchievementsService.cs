using IA;
using IA.Events;
using IA.SDK;
using IA.SDK.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using System.Collections.Generic;
using System.Linq;

namespace Miki.Modules.Accounts.Services
{
    internal class AchievementsService : BaseService
    {
        public AchievementsService()
        {
            Name = "Achievements";
        }

        public override void Install(IModule m)
        {
            base.Install(m);
            LoadAchievements();
        }

        public override void Uninstall(IModule m)
        {
            base.Uninstall(m);
        }

        public void LoadAchievements()
        {
            AchievementDataContainer<AchievementAchievement> AchievementAchievements = new AchievementDataContainer<AchievementAchievement>(x =>
             {
                 x.Name = "achievements";
                 x.Achievements = new List<AchievementAchievement>()
                {
                    new AchievementAchievement()
                    {
                        Name = "Underachiever",
                        Icon = "✏️",
                        CheckAchievement = async (p) =>
                        {
                            return p.count > 3;
                        },
                    },
                    new AchievementAchievement()
                    {
                        Name = "Achiever",
                        Icon = "🖊️",
                        CheckAchievement = async (p) =>
                        {
                            return p.count > 5;
                        },
                    },
                    new AchievementAchievement()
                    {
                    Name = "Overachiever",
                    Icon = "🖋️",
                    CheckAchievement = async (p) =>
                    {
                        return p.count > 12;
                    },
                }
            };
             });

            AchievementDataContainer<CommandAchievement> InfoAchievement = new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "info";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Informed",
                        Icon = "📚",

                        CheckCommand = async (p) =>
                        {
                            return p.command.Name.ToLower() == "info";
                        }
                    }
                };
            });
            AchievementDataContainer<CommandAchievement> LonelyAchievement = new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "fa";
                x.Achievements = new List<CommandAchievement>()
                        {
                            new CommandAchievement()
                            {
                                Name = "Lonely",
                                Icon = "😭",

                                CheckCommand = async (p) =>
                                {
                                        return p.command.Name.ToLower() == "marry" && p.message.MentionedUserIds.First() == p.message.Author.Id;
                                }
                            }
                        };
            });
            AchievementDataContainer<CommandAchievement> ChefAchievement = new AchievementDataContainer<CommandAchievement>(x =>
            {
                x.Name = "creator";
                x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Chef",
                        Icon = "📝",
                        CheckCommand = async (p) =>
                        {
                            if(p.command.Name.ToLower() == "createpasta")
                            {
                                return true;
                            }
                            return false;
                        }
                    }
                };
            });
            AchievementDataContainer<CommandAchievement> NoPermissionAchievement = new AchievementDataContainer<CommandAchievement>(x =>
             {
                 x.Name = "noperms";
                 x.Achievements = new List<CommandAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "NO! Don't touch that!",
                        Icon = "😱",
                        CheckCommand = async (p) =>
                        {
                            return Bot.instance.Events.CommandHandler.GetUserAccessibility(p.message) < p.command.Accessibility;
                        },
                    }
                };
             });

            AchievementDataContainer<LevelAchievement> LevelAchievement = new AchievementDataContainer<LevelAchievement>(x =>
             {
                 x.Name = "levelachievements";
                 x.Achievements = new List<LevelAchievement>()
                 {
                    new LevelAchievement()
                    {
                        Name = "Novice",
                        Icon = "🎟",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 3;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Intermediate",
                        Icon = "🎫",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 5;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Experienced",
                        Icon = "🏵",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 10;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Expert",
                        Icon = "🎗",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 20;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Sage",
                        Icon = "🎖",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 30;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Master",
                        Icon = "🏅",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 50;
                        }
                    },
                    new LevelAchievement()
                    {
                        Name = "Legend",
                        Icon = "💮",
                        CheckLevel = async (p) =>
                        {
                            return p.level >= 75;
                        }
                    }
                 };
             });

            AchievementDataContainer<MessageAchievement> FrogAchievement = new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "frog";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Oh shit! Waddup",
                        Icon = "🐸",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Contains("dat boi");
                        }
                    }
                };
            });
            AchievementDataContainer<MessageAchievement> LennyAchievement = new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "lenny";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lenny",
                        Icon = "😏",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Contains("( ͡° ͜ʖ ͡°)");
                        }
                    }
                };
            });
            AchievementDataContainer<MessageAchievement> PoiAchievement = new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "poi";
                x.Achievements = new List<MessageAchievement>
                {
                    new MessageAchievement()
                    {
                        Name = "Shipgirl",
                        Icon = "⛵",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Split(' ').Contains("poi");
                        },
                    }
                };
            });
            AchievementDataContainer<MessageAchievement> LuckyAchievement = new AchievementDataContainer<MessageAchievement>(x =>
            {
                x.Name = "goodluck";
                x.Achievements = new List<MessageAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lucky",
                        Icon = "🍀",
                        CheckMessage = async (p) =>
                        {
                            return (Global.random.Next(0, 10000000) == 5033943);
                        },
                    }
                };
            });

            AchievementDataContainer<TransactionAchievement> MekosAchievement = new AchievementDataContainer<TransactionAchievement>(x =>
            {
                x.Name = "meko";
                x.Achievements = new List<TransactionAchievement>()
                {
                    new TransactionAchievement()
                    {
                        Name = "Loaded",
                        Icon = "💵",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 10000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Rich",
                        Icon = "💸",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 50000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Minted",
                        Icon = "💲",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 125000;
                        }
                    },
                    new TransactionAchievement()
                    {
                        Name = "Millionaire",
                        Icon = "🤑",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 1000000;
                        }
                    }
                };
            });

            #region Achievement Achievements

            AchievementManager.Instance.OnAchievementUnlocked += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await AchievementAchievements.CheckAsync(pa);
                }
            };

            #endregion Achievement Achievements

            #region Command Achievements

            AchievementManager.Instance.OnCommandUsed += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await InfoAchievement.CheckAsync(pa);
                }
            };
            AchievementManager.Instance.OnCommandUsed += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await LonelyAchievement.CheckAsync(pa);
                }
            };
            AchievementManager.Instance.OnCommandUsed += async (pa) =>
            {
                await ChefAchievement.CheckAsync(pa);
            };
            AchievementManager.Instance.OnCommandUsed += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await NoPermissionAchievement.CheckAsync(pa);
                }
            };

            #endregion Command Achievements

            #region Level Achievements

            AchievementManager.Instance.OnLevelGained += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await LevelAchievement.CheckAsync(pa);
                }
            };

            #endregion Level Achievements

            #region Message Achievements

            AchievementManager.Instance.OnMessageReceived += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await FrogAchievement.CheckAsync(pa);
                }
            };
            AchievementManager.Instance.OnMessageReceived += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await LennyAchievement.CheckAsync(pa);
                }
            };
            AchievementManager.Instance.OnMessageReceived += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await PoiAchievement.CheckAsync(pa);
                }
            };
            AchievementManager.Instance.OnMessageReceived += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await LuckyAchievement.CheckAsync(pa);
                }
            };

            #endregion Message Achievements

            #region Misc Achievements

            new AchievementDataContainer<BaseAchievement>(x =>
            {
                x.Name = "badluck";
                x.Achievements = new List<BaseAchievement>()
                {
                    new BaseAchievement()
                    {
                        Name = "Unlucky",
                        Icon = "🎲"
                    }
                };
            });

            #endregion Misc Achievements

            #region User Update Achievements (don't disable these)

            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "contributor";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Contributor",
                        Icon = "⭐",

                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Contributors"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "developer";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Developer",
                        Icon = "🌟",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Developer"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "glitch";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Glitch",
                        Icon = "👾",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Succesfully broke Miki"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;
            AchievementManager.Instance.OnUserUpdate += new AchievementDataContainer<UserUpdateAchievement>(x =>
            {
                x.Name = "donator";
                x.Achievements = new List<UserUpdateAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Donator",
                        Icon = "💖",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.userNew.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Donators"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                };
            }).CheckAsync;

            #endregion User Update Achievements (don't disable these)

            #region Transaction Achievements

            AchievementManager.Instance.OnTransaction += async (pa) =>
            {
                if (await IsEnabled(pa.discordChannel.Id))
                {
                    await MekosAchievement.CheckAsync(pa);
                }
            };

            #endregion Transaction Achievements
        }
    }
}