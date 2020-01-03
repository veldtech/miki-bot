
namespace Miki.Modules.Accounts.Services
{
    using Miki.Services.Achievements;

    public class AchievementIds
    {
        public static string AchievementsId { get; } = "achievements";
        public static string LotteryWinId { get; } = "lottery";
        public static string ReadInfoId { get; } = "info";
        public static string MarrySelfId { get; } = "fa";
        public static string CreatePastaId { get; } = "creator";
        public static string InvalidPermsId { get; } = "noperms";
        public static string LevellingId { get; } = "levelachievements";
        public static string LuckId { get; } = "goodluck";
        public static string CurrencyId { get; } = "meko";
        public static string VoteId { get; } = "voter";
        public static string SlotsId { get; } = "slots";
        public static string UnluckyId { get; } = "badluck";
        public static string StaffId { get; } = "contributor";
        public static string DeveloperId { get; } = "developer";
        public static string BugtesterId { get; } = "glitch";
        public static string DonatorId { get; } = "donator";

        // memes
        public static string FrogId { get; } = "frog";
        public static string LennyId { get; } = "lenny";
        public static string ShipId { get; } = "poi";
        public static string LewdId { get; set; } = "lewd";
    }

    public class AchievementLoader
    {
        public AchievementLoader(AchievementService service)
        {
            LoadAchievements(service);
        }

        private void LoadAchievements(AchievementService service)
        {
            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.AchievementsId)
                    .AddEntry("Underachiever", "🖍")
                    .AddEntry("Achiever", "✏️")
                    .AddEntry("Completionist", "🖊️")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.LotteryWinId)
                    .AddEntry("Celebrator", "🍺")
                    .AddEntry("Absolute Madman", "🍸")
                    .AddEntry("Pop da champagne", "🍾")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.ReadInfoId)
                    .AddEntry("Informed", "📚")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.MarrySelfId)
                    .AddEntry("Lonely", "😭")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.CreatePastaId)
                    .AddEntry("Chef", "📝")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.InvalidPermsId)
                    .AddEntry("NO! Don't touch that!", "😱")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.LevellingId)
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
                new AchievementObject.Builder(AchievementIds.FrogId)
                    .AddEntry("Oh shit! Waddup", "🐸")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.LennyId)
                    .AddEntry("Lenny", "😏")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.ShipId)
                    .AddEntry("Shipgirl", "⛵")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.LuckId)
                    .AddEntry("Lucky", "🍀")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.CurrencyId)
                    .AddEntry("Loaded", "💵")
                    .AddEntry("Rich", "💸")
                    .AddEntry("Minted", "💲")
                    .AddEntry("Millionaire", "🤑")
                    .AddEntry("Billionaire", "🏦")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.VoteId)
                    .AddEntry("Helper", "✉")
                    .AddEntry("Voter", "🗳")
                    .AddEntry("Elector", "🗃")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.SlotsId)
                    .AddEntry("Jackpot", "🎰")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.UnluckyId)
                    .AddEntry("Unlucky", "🎲")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.StaffId)
                    .AddEntry("Contributor", "⭐")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.DeveloperId)
                    .AddEntry("Developer", "🌟")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.BugtesterId)
                    .AddEntry("Glitch", "👾")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.DonatorId)
                    .AddEntry("Donator", "💖")
                    .AddEntry("Supporter", "💘")
                    .AddEntry("Sponsor", "💟")
                    .Build());

            service.AddAchievement(
                new AchievementObject.Builder(AchievementIds.LewdId)
                    .AddEntry("Lewd", "💋")
                    .Build());
        }
    }
}