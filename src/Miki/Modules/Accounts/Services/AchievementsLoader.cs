
namespace Miki.Modules.Accounts.Services
{
    using Miki.Services.Achievements;
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
                new AchievementObject.Builder("achievements")
                    .AddEntry("Underachiever", "🖍")
                    .AddEntry("Achiever", "✏️")
                    .AddEntry("Completionist", "🖊️")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("lottery")
                    .AddEntry("Celebrator", "🍺")
                    .AddEntry("Absolute Madman", "🍸")
                    .AddEntry("Pop da champagne", "🍾")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("info")
                    .AddEntry("Informed", "📚")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("fa")
                    .AddEntry("Lonely", "😭")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("creator")
                    .AddEntry("Chef", "📝")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("noperms")
                    .AddEntry("NO! Don't touch that!", "😱")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("levelachievements")
                    .AddEntry("Novice", "🎟")
                    .AddEntry("Intermediate", "🎫")
                    .AddEntry("Experienced", "🏵")
                    .AddEntry("Expert", "🎗")
                    .AddEntry("Sage", "🎖")
                    .AddEntry("Master", "🏅")
                    .AddEntry("Legend", "💮")
                    .AddEntry("Epic", "🌸")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("frog")
                    .AddEntry("Oh shit! Waddup", "🐸")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("lenny")
                    .AddEntry("Lenny", "😏")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("poi")
                    .AddEntry("Shipgirl", "⛵")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("goodluck")
                    .AddEntry("Lucky", "🍀")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("meko")
                    .AddEntry("Loaded", "💵")
                    .AddEntry("Rich", "💸")
                    .AddEntry("Minted", "💲")
                    .AddEntry("Millionaire", "🤑")
                    .AddEntry("Billionaire", "🏦")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("voter")
                    .AddEntry("Helper", "✉")
                    .AddEntry("Voter", "🗳")
                    .AddEntry("Elector", "🗃")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("slots")
                    .AddEntry("Jackpot", "🎰")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("badluck")
                    .AddEntry("Unlucky", "🎲")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("contributor")
                    .AddEntry("Contributor", "⭐")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("developer")
                    .AddEntry("Developer", "🌟")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("glitch")
                    .AddEntry("Glitch", "👾")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder("donator")
                    .AddEntry("Donator", "💖")
                    .AddEntry("Supporter", "💘")
                    .AddEntry("Sponsor", "💟")
                    .Build());
        }
    }
}