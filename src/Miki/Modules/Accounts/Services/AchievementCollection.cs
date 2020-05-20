using System;
using System.Collections.Generic;
using Miki.Services.Achievements;

namespace Miki.Modules.Accounts.Services
{
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

    public class AchievementCollection
    {
        private readonly Dictionary<string, AchievementObject> containers
            = new Dictionary<string, AchievementObject>();

        public AchievementCollection()
        {
            LoadAchievements();
        }

        public void AddAchievement(AchievementObject @object)
        {
            if(containers.ContainsKey(@object.Id))
            {
                throw new ArgumentException(
                    $"Achievement with name '{@object.Id}' already exists.");
            }
            containers.Add(@object.Id, @object);
        }

        public AchievementObject GetAchievementOrDefault(string achievementId)
        {
            if(TryGetAchievement(achievementId, out var value))
            {
                return value;
            }
            return null;
        }

        private void LoadAchievements()
        {
            AddAchievement(
                new AchievementObject.Builder(AchievementIds.AchievementsId)
                    .Add("🖍").Add("✏️").Add("🖊️").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.LotteryWinId)
                    .Add("🍺").Add("🍸").Add("🍾").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.ReadInfoId)
                    .Add("📚").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.MarrySelfId)
                    .Add("😭").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.CreatePastaId)
                    .Add("📝").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.InvalidPermsId)
                    .Add("😱").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.LevellingId)
                    .Add("🎟")
                    .Add("🎫")
                    .Add("🏵")
                    .Add("🎗")
                    .Add("🎖")
                    .Add("🏅")
                    .Add("💮")
                    .Add("🌸")
                    .Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.FrogId)
                    .Add("🐸").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.LennyId)
                    .Add("😏").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.ShipId)
                    .Add("⛵").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.LuckId)
                    .Add("🍀").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.CurrencyId)
                    .Add("💵").Add("💸").Add("💲").Add("🤑").Add("🏦").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.VoteId)
                    .Add("✉").Add("🗳").Add("🗃").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.SlotsId)
                    .Add("🎰").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.UnluckyId)
                    .Add("🎲").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.StaffId)
                    .Add("⭐").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.DeveloperId)
                    .Add("🌟").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.BugtesterId)
                    .Add("👾").Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.DonatorId)
                    .Add("💖")
                    .Add("💘")
                    .Add("💟")
                    .Build());

            AddAchievement(
                new AchievementObject.Builder(AchievementIds.LewdId)
                    .Add("💋").Build());
        }

        public bool TryGetAchievement(string achievementId, out AchievementObject @object)
        {
            if(containers.ContainsKey(achievementId))
            {
                @object = containers[achievementId];
                return true;
            }

            @object = null;
            return false;
        }
    }
}