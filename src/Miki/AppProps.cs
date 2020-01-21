using System;
using System.Collections.Generic;
using System.Text;

namespace Miki
{
    /// <summary>
    /// Static Application Properties
    /// </summary>
    public static class AppProps
    {
        public static class Emoji
        {
            public static string Mekos => "<:mekos:421972155484471296>";

            public static string ExpBarOnStart 
                => "<:mbarlefton:391971424442646534>";
            public static string ExpBarOnMiddle 
                => "<:mbarmidon:391971424920797185>";
            public static string ExpBarOnEnd 
                => "<:mbarrighton:391971424488783875>";
            public static string ExpBarOffStart 
                => "<:mbarleftoff:391971424824459265>";
            public static string ExpBarOffMiddle 
                => "<:mbarmidoff:391971424824197123>";
            public static string ExpBarOffEnd 
                => "<:mbarrightoff:391971424862208000>";
        }

        public class Currency
        {
            public static long BankId => 1L;
        }
    }                 
}
