namespace Miki
{
    /// <summary>
    /// Static Application Properties
    /// </summary>
    public static class AppProps
    {
        public static class Emoji
        {
            public static string Disabled => "🚫";
            public static string ExpBarOnStart => "<:mbarlefton:391971424442646534>";
            public static string ExpBarOnMiddle => "<:mbarmidon:391971424920797185>";
            public static string ExpBarOnEnd => "<:mbarrighton:391971424488783875>";
            public static string ExpBarOffStart => "<:mbarleftoff:391971424824459265>";
            public static string ExpBarOffMiddle => "<:mbarmidoff:391971424824197123>";
            public static string ExpBarOffEnd => "<:mbarrightoff:391971424862208000>";
            public static string Mekos => "<:mekos:421972155484471296>";
            public static string Reddit => "<:reddit:704006214694076427>";
            public static string WeeklyEmbedIcon => ":house:";
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
