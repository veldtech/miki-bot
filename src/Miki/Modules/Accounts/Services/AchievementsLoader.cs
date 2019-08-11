
namespace Miki.Modules.Accounts.Services
{
    using Miki.Services.Achievements;
    using Miki.Accounts.Achievements.Objects;
    using System.Linq;
    using System.Threading.Tasks;

    public class AchievementLoader
    {
        public AchievementLoader(
            AchievementService service)
        {
            LoadAchievements(service);
        }

        private void LoadAchievements(AchievementService service)
        {
            service.AddAchievement(
                "achievements",
                new AchievementAchievement
                {
                    Name = "Underachiever",
                    Icon = "🖍",
                    CheckAchievement = (p) => new ValueTask<bool>(p.count >= 3),
                    Points = 5,
                },
                new AchievementAchievement
                {
                    Name = "Achiever",
                    Icon = "✏️",
                    CheckAchievement = (p) => new ValueTask<bool>(p.count >= 5),
                    Points = 10,
                },
                new AchievementAchievement
                {
                    Name = "Completionist",
                    Icon = "🖊️",
                    CheckAchievement = (p) => new ValueTask<bool>(p.count >= 25),
                    Points = 30,
                });

            service.AddAchievement(
                "lottery",
                new ManualAchievement
                {
                    Name = "Celebrator",
                    Icon = "🍺",
                    Points = 5,
                },
                new ManualAchievement
                {
                    Name = "Absolute Madman",
                    Icon = "🍸",
                    Points = 10,
                },
                new ManualAchievement
                {
                    Name = "Pop da champagne",
                    Icon = "🍾",
                    Points = 15
                });

            service.AddAchievement(
                "info",
                new CommandAchievement
                {
                    Name = "Informed",
                    Icon = "📚",
                    CheckCommand = (p) => new ValueTask<bool>(p.command.Metadata.Identifiers
                        .Contains("info")),
                    Points = 5,
                });

            service.AddAchievement(
                "fa",
                new CommandAchievement
                {
                    Name = "Lonely",
                    Icon = "😭",
                    CheckCommand = (p) => new ValueTask<bool>(
                        p.command.Metadata.Identifiers.Contains("marry")
                        && p.message.MentionedUserIds.FirstOrDefault() == p.discordUser.Id),
                    Points = 5,
                });

            service.AddAchievement(
                "creator",
                new CommandAchievement
                {
                    Name = "Chef",
                    Icon = "📝",
                    CheckCommand = (p) => new ValueTask<bool>(p.command
                        .Metadata
                        .Identifiers
                        .Contains("createpasta")),
                    Points = 5,
                });

            service.AddAchievement(
                "noperms",
                new CommandAchievement
                {
                    Name = "NO! Don't touch that!",
                    Icon = "😱",
                    CheckCommand = (p) =>
                    {
                        // TODO(@velddev): Reimplement with new framework.
                        return new ValueTask<bool>(false);
                        //return await MikiApp.Instance.GetService<EventSystem>().GetCommandHandler<SimpleCommandHandler>().GetUserAccessibility(p.message, p.discordChannel as IDiscordGuildChannel) < p.command.Accessibility;
                    },
                    Points = 5
                });

            service.AddAchievement(
                "levelachievements",
                new LevelAchievement
                {
                    Name = "Novice",
                    Icon = "🎟",
                    CheckLevel = async (p) => p.level >= 3,
                    Points = 5,
                }, 
                new LevelAchievement
                {
                    Name = "Intermediate",
                    Icon = "🎫",
                    CheckLevel = async (p) => p.level >= 5,
                    Points = 10,
                },
                new LevelAchievement
                {
                    Name = "Experienced",
                    Icon = "🏵",
                    CheckLevel = async (p) => p.level >= 10,
                    Points = 15,
                },
                new LevelAchievement
                {
                    Name = "Expert",
                    Icon = "🎗",
                    CheckLevel = async (p) => p.level >= 20,
                    Points = 20,
                },
                new LevelAchievement
                {
                    Name = "Sage",
                    Icon = "🎖",
                    CheckLevel = async (p) => p.level >= 30,
                    Points = 25,
                },
                new LevelAchievement
                {
                    Name = "Master",
                    Icon = "🏅",
                    CheckLevel = async (p) => p.level >= 50,
                    Points = 30,
                },
                new LevelAchievement
                {
                    Name = "Legend",
                    Icon = "💮",
                    CheckLevel = async (p) => p.level >= 100,
                    Points = 35,
                },
                new LevelAchievement
                {
                    Name = "Epic",
                    Icon = "🌸",
                    CheckLevel = async (p) => p.level >= 150,
                    Points = 40,
                });

            service.AddAchievement(
                "frog",
                new CommandAchievement
                {
                    Name = "Oh shit! Waddup",
                    Icon = "🐸",
                    CheckCommand = (p) => new ValueTask<bool>(
                        p.command.Metadata.Identifiers.Contains("pasta")
                        && p.message.Content.Contains("dat boi")),
                    Points = 5
                });

            service.AddAchievement(
                "lenny",
                new CommandAchievement
                {
                    Name = "Lenny",
                    Icon = "😏",
                    CheckCommand = (p) => new ValueTask<bool>(
                        p.command.Metadata.Identifiers.Contains("pasta")
                        && p.message.Content.Contains("( ͡° ͜ʖ ͡°)")),
                    Points = 5
                });

            service.AddAchievement(
                "poi",
                new CommandAchievement
                {
                    Name = "Shipgirl",
                    Icon = "⛵",
                    CheckCommand = (p) => new ValueTask<bool>(
                        p.command.Metadata.Identifiers.Contains("pasta")
                        && p.message.Content.Split(' ').Contains("poi")),
                    Points = 5,
                });

            service.AddAchievement(
                "goodluck",
                new CommandAchievement()
                {
                    Name = "Lucky",
                    Icon = "🍀",
                    CheckCommand = async (p) => (MikiRandom.Next(0, 10000000) == 5033943),
                    Points = 25
                });

            service.AddAchievement(
                "meko",
                new TransactionAchievement
                {
                    Name = "Loaded",
                    Icon = "💵",
                    CheckTransaction = (p) => new ValueTask<bool>(p.receiver.Currency > 10000),
                    Points = 5
                },
                new TransactionAchievement
                {
                    Name = "Rich",
                    Icon = "💸",
                    CheckTransaction = (p) => new ValueTask<bool>(p.receiver.Currency > 50000),
                    Points = 10
                },
                new TransactionAchievement
                {
                    Name = "Minted",
                    Icon = "💲",
                    CheckTransaction = (p) => new ValueTask<bool>(p.receiver.Currency > 125000),
                    Points = 15
                },
                new TransactionAchievement
                {
                    Name = "Millionaire",
                    Icon = "🤑",
                    CheckTransaction = (p) => new ValueTask<bool>(p.receiver.Currency > 1000000),
                    Points = 20
                },
                new TransactionAchievement
                {
                    Name = "Billionaire",
                    Icon = "🏦",
                    CheckTransaction = (p) => new ValueTask<bool>(p.receiver.Currency > 1000000000),
                    Points = 25
                });

            service.AddAchievement(
                "voter",
                new ManualAchievement
                {
                    Name = "Helper",
                    Icon = "✉",
                    Points = 5,
                },
                new ManualAchievement
                {
                    Name = "Voter",
                    Icon = "🗳",
                    Points = 10,
                },
                new ManualAchievement
                {
                    Name = "Elector",
                    Icon = "🗃",
                    Points = 15,
                });

            service.AddAchievement(
                "slots",
                new ManualAchievement
                {
                    Name = "Jackpot",
                    Icon = "🎰",
                    Points = 15
                });

            service.AddAchievement(
                "badluck",
                new ManualAchievement()
                {
                    Name = "Unlucky",
                    Icon = "🎲",
                    Points = 5
                });

            service.AddAchievement(
                "contributor",
                new UserUpdateAchievement()
                {
                    Name = "Contributor",
                    Icon = "⭐",
                    CheckUserUpdate = async (p) =>
                    {
                        return false;
                    }
                });

            service.AddAchievement(
                "developer",
                new UserUpdateAchievement()
                {
                    Name = "Developer",
                    Icon = "🌟",
                    CheckUserUpdate = async (p) =>
                    {
                        return false;
                    }
                });

            service.AddAchievement(
                "glitch",
                new UserUpdateAchievement()
                {
                    Name = "Glitch",
                    Icon = "👾",
                    CheckUserUpdate = async (p) =>
                    {
                        return false;
                    }
                });

            service.AddAchievement(
                "donator",
                new ManualAchievement
                {
                    Name = "Donator",
                    Icon = "💖",
                    Points = 0,
                },
                new ManualAchievement
                {
                    Name = "Supporter",
                    Icon = "💘",
                    Points = 0,
                },
                new ManualAchievement
                {
                    Name = "Sponsor",
                    Icon = "💟",
                    Points = 0,
                });
        }
    }
}