namespace Miki
{
    /// <summary>
    /// Static Application Properties
    /// </summary>
    public static class AppProps
    {
        public static class Emoji
        {
            public const string Disabled = "🚫";
            public const string ExpBarOnStart = "<:mbarlefton:391971424442646534>";
            public const string ExpBarOnMiddle = "<:mbarmidon:391971424920797185>";
            public const string ExpBarOnEnd = "<:mbarrighton:391971424488783875>";
            public const string ExpBarOffStart = "<:mbarleftoff:391971424824459265>";
            public const string ExpBarOffMiddle = "<:mbarmidoff:391971424824197123>";
            public const string ExpBarOffEnd = "<:mbarrightoff:391971424862208000>";
            public const string Mekos = "<:mekos:421972155484471296>";
            public const string Reddit = "<:reddit:704006214694076427>";
            public const string WeeklyEmbedIcon = "🏠";

            public const string Ledger = "📒";
            public const string Running = "🏃‍";
            public const string HammerAndWrench = "🛠";
            public const string Television = "📺";
            public const string Wrench = "🔧";
            public const string SpaceInvader = "👾";
            public const string MoneyBill = "💵";
            public const string RollerCoaster = "🎢";
            public const string Slots = "🎰";
            public const string GamePad = "🎮";
            public const string SpeechBubble = "💬";
            public const string Developer = "👨‍💻";
            public const string Ring = "💍";
            public const string HotFace = "🥵";
            public const string Spaghetti = "🍝";
            public const string ScreamFace = "😱";
            public const string Hammer = "🔨";
            public const string Digits = "🔢";
            public const string Gear = "⚙";
        }

        public static class Currency
        {
            public static long BankId => 1L;
        }

        public static class Daily
        {
            public static int DailyAmount => 100;
            public static int StreakAmount => 20;
        }

        public static class Links
        {
            public static string DiscordInvite = 
                "https://discordapp.com/oauth2/authorize" 
                + "?client_id=160185389313818624&scope=bot&permissions=355593334";

            /// <summary>
            /// Invite or redirect to the language page.
            /// </summary>
            public static string LocalizationInvite =
                "https://translations.miki.ai/engage/miki/";
        }
    }                 
}
