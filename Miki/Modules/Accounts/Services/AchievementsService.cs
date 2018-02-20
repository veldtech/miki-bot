using Miki.Framework;
using Miki.Framework.Events;
using Miki.Common;
using Miki.Common.Interfaces;
using Miki.Accounts.Achievements;
using Miki.Accounts.Achievements.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable CS1998

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
            AchievementManager.Instance.provider = this;
            LoadAchievements();
        }

        public override void Uninstall(IModule m)
        {
            base.Uninstall(m);
        }

        public void LoadAchievements()
        {
            AchievementDataContainer AchievementAchievements = new AchievementDataContainer(x =>
             {
                 x.Name = "achievements";
                 x.Achievements = new List<BaseAchievement>()
                {
                    new AchievementAchievement()
                    {
                        Name = "Underachiever",
                        Icon = "✏️",
                        CheckAchievement = async (p) =>
                        {
							await Task.Yield();
                            return p.count > 3;
                        },
                    },
                    new AchievementAchievement()
                    {
                        Name = "Achiever",
                        Icon = "🖊️",
                        CheckAchievement = async (p) =>
                        {
							await Task.Yield();
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

            AchievementDataContainer InfoAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "info";
                x.Achievements = new List<BaseAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Informed",
                        Icon = "📚",

                        CheckCommand = async (p) =>
                        {
							await Task.Yield();
							return p.command.Name.ToLower() == "info";
                        },
						Points = 5
                    }
                };
            });
            AchievementDataContainer LonelyAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "fa";
                x.Achievements = new List<BaseAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Lonely",
                        Icon = "😭",

                        CheckCommand = async (p) =>
                        {
							await Task.Yield();
							return p.command.Name.ToLower() == "marry" && p.message.MentionedUserIds.First() == p.message.Author.Id;
                        },
						Points = 5,
                    }
                };
            });
            AchievementDataContainer ChefAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "creator";
                x.Achievements = new List<BaseAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "Chef",
                        Icon = "📝",
                        CheckCommand = async (p) =>
                        {
							await Task.Yield();
							return p.command.Name.ToLower() == "createpasta";
                        },
						Points = 5,
                    }
                };
            });
            AchievementDataContainer NoPermissionAchievement = new AchievementDataContainer(x =>
             {
                 x.Name = "noperms";
                 x.Achievements = new List<BaseAchievement>()
                {
                    new CommandAchievement()
                    {
                        Name = "NO! Don't touch that!",
                        Icon = "😱",
                        CheckCommand = async (p) =>
                        {
							await Task.Yield();
							return EventSystem.Instance.CommandHandler.GetUserAccessibility(p.message) < p.command.Accessibility;
                        },
						Points = 0
                    }
                };
             });

            AchievementDataContainer LevelAchievement = new AchievementDataContainer(x =>
             {
                 x.Name = "levelachievements";
                 x.Achievements = new List<BaseAchievement>()
                 {
                    new LevelAchievement()
                    {
                        Name = "Novice",
                        Icon = "🎟",
                        CheckLevel = async (p) => p.level >= 3,
						Points = 5,
                    },
                    new LevelAchievement()
                    {
                        Name = "Intermediate",
                        Icon = "🎫",
                        CheckLevel = async (p) => p.level >= 5,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Experienced",
                        Icon = "🏵",
                        CheckLevel = async (p) => p.level >= 10,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Expert",
                        Icon = "🎗",
                        CheckLevel = async (p) => p.level >= 20,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Sage",
                        Icon = "🎖",
                        CheckLevel = async (p) => p.level >= 30,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Master",
                        Icon = "🏅",
                        CheckLevel = async (p) => p.level >= 50,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Legend",    
                        Icon = "💮",
                        CheckLevel = async (p) => p.level >= 100,
						Points = 5,
					},
                    new LevelAchievement()
                    {
                        Name = "Epic",
                        Icon = "🌸",
                        CheckLevel = async (p) => p.level >= 150,
						Points = 5,
					}
                 };
             });

            AchievementDataContainer FrogAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "frog";
                x.Achievements = new List<BaseAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Oh shit! Waddup",
                        Icon = "🐸",
                        CheckMessage = async (p) => p.message.Content.Contains("dat boi"),
						Points = 5
                    }
                };
            });
            AchievementDataContainer LennyAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "lenny";   
                x.Achievements = new List<BaseAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lenny",
                        Icon = "😏",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Contains("( ͡° ͜ʖ ͡°)");
                        },
						Points = 5
                    }
                };
            });
            AchievementDataContainer PoiAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "poi";
                x.Achievements = new List<BaseAchievement>
                {
                    new MessageAchievement()
                    {
                        Name = "Shipgirl",
                        Icon = "⛵",
                        CheckMessage = async (p) =>
                        {
                            return p.message.Content.Split(' ').Contains("poi");
                        },
						Points = 5,
                    }
                };
            });
            AchievementDataContainer LuckyAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "goodluck";
                x.Achievements = new List<BaseAchievement>()
                {
                    new MessageAchievement()
                    {
                        Name = "Lucky",
                        Icon = "🍀",
                        CheckMessage = async (p) =>
                        {
                            return (MikiRandom.Next(0, 10000000) == 5033943);
                        },
						Points = 25
                    }
                };
            });

            AchievementDataContainer MekosAchievement = new AchievementDataContainer(x =>
            {
                x.Name = "meko";
                x.Achievements = new List<BaseAchievement>()
                {
                    new TransactionAchievement()
                    {
                        Name = "Loaded",
                        Icon = "💵",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 10000;
                        },
						Points = 5
                    },
                    new TransactionAchievement()
                    {
                        Name = "Rich",
                        Icon = "💸",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 50000;
                        },
						Points = 5
                    },
                    new TransactionAchievement()
                    {
                        Name = "Minted",
                        Icon = "💲",
                        CheckTransaction = async (p) =>
						{
							return p.receiver.Currency > 125000;
						},
						Points = 10
                    },
                    new TransactionAchievement()
                    {
                        Name = "Millionaire",
                        Icon = "🤑",
                        CheckTransaction = async (p) =>
                        {
                            return p.receiver.Currency > 1000000;
                        },
						Points = 25
                    }
                };
            });

			AchievementDataContainer DiscordBotsOrgAchievement = new AchievementDataContainer()
			{
				Name = "supporter",
				Achievements = new List<BaseAchievement>()
				{
					new BaseAchievement()
					{
						Name = "Supporter",
						Icon = "",
						Points = 10,
					}
				}
			};

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

            AchievementManager.Instance.OnCommandUsed += InfoAchievement.CheckAsync;
            AchievementManager.Instance.OnCommandUsed += LonelyAchievement.CheckAsync;
            AchievementManager.Instance.OnCommandUsed += ChefAchievement.CheckAsync;
            AchievementManager.Instance.OnCommandUsed += NoPermissionAchievement.CheckAsync;
            #endregion Command Achievements

            #region Level Achievements
            AchievementManager.Instance.OnLevelGained += LevelAchievement.CheckAsync;

            #endregion Level Achievements

            #region Misc Achievements

            new AchievementDataContainer(x =>
            {
                x.Name = "badluck";
                x.Achievements = new List<BaseAchievement>()
                {
                    new BaseAchievement()
                    {
                        Name = "Unlucky",
                        Icon = "🎲",
						Points = 5
                    }
                };
				
            });

            #endregion Misc Achievements

            #region User Update Achievements (don't disable these)

            new AchievementDataContainer(x =>
            {
                x.Name = "contributor";
                x.Achievements = new List<BaseAchievement>()
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
            });
            new AchievementDataContainer(x =>
            {
                x.Name = "developer";
                x.Achievements = new List<BaseAchievement>()
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
            });
            new AchievementDataContainer(x =>
            {
                x.Name = "glitch";
                x.Achievements = new List<BaseAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Glitch",
                        Icon = "👾",
                        CheckUserUpdate = async (p) =>
                        {
                            return false;
                        }
                    }
                };
            });
            new AchievementDataContainer(x =>
            {
                x.Name = "donator";
                x.Achievements = new List<BaseAchievement>()
                {
                    new UserUpdateAchievement()
                    {
                        Name = "Donator",
                        Icon = "💖",
                        CheckUserUpdate = async (p) =>
                        {
                            if (p.discordUser.Guild.Id == 160067691783127041)
                            {
                                IDiscordRole role = p.userNew.Guild.Roles.Find(r => { return r.Name == "Donators"; });

                                if (p.userNew.RoleIds.Contains(role.Id))
                                {
                                    return true;
                                }
                            }
                            return false;
                        },
						Points = 0,
                    }
                };
            });

            #endregion User Update Achievements (don't disable these)

            #region Transaction Achievements

            AchievementManager.Instance.OnTransaction += MekosAchievement.CheckAsync;

            #endregion Transaction Achievements
        }
    }
}
