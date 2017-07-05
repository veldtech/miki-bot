using IA;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Miki.Languages
{
    // Class
    public partial class Locale
    {
        public static Dictionary<string, ResourceManager> Locales = new Dictionary<string, ResourceManager>();

        private string defaultResource = "en-us";
        private long id;

        private Locale()
        {
        }
        private Locale(long id)
        {
            this.id = id;
        }

        public static void Load()
        {
            Locales.Add("ar-ae", ar_AE.ResourceManager);

            Locales.Add("cz-cz", cz_CZ.ResourceManager);

            Locales.Add("da-dk", da_DK.ResourceManager);
            Locales.Add("de-de", de_DE.ResourceManager);

            Locales.Add("en-us", en_US.ResourceManager);
            Locales.Add("es-es", es_ES.ResourceManager);

            Locales.Add("fr-fr", fr_FR.ResourceManager);

            Locales.Add("ja-ja", ja.ResourceManager);

            Locales.Add("nl-nl", nl_NL.ResourceManager);

            Locales.Add("pt-pt", pt_PT.ResourceManager);

            Locales.Add("sv-se", sv_SE.ResourceManager);

            Locales.Add("zh-chs", zh_CHS.ResourceManager);
            Locales.Add("zh-cht", zh_CHT.ResourceManager);
        }

        public static Locale GetEntity(ulong id) => GetEntity(id.ToDbLong());
        public static Locale GetEntity(long id)
        {
            return new Locale(id);
        }

        public bool HasString(string m)
        {
            using (var context = new MikiContext())
            {
                ChannelLanguage l = context.Languages.Find(id);

                string lang;

                if (l == null)
                {
                    lang = defaultResource;
                }
                else
                {
                    lang = l.Language;
                }

                string output = Locales[lang].GetString(m);

                if (string.IsNullOrWhiteSpace(output))
                {
                    output = Locales[defaultResource].GetString(m);
                }

                return !string.IsNullOrWhiteSpace(output);
            }
        }

        public string GetString(string m, params object[] p)
        {
            using (var context = new MikiContext())
            {
                ChannelLanguage l = context.Languages.Find(id);

                string lang;

                if (l == null)
                {
                    lang = defaultResource;
                }
                else
                {
                    lang = l.Language;
                }

                ResourceManager resources = Locales[lang];
                string output = null;

                if (InternalStringAvailable(m, resources))
                {
                    output = InternalGetString(m, resources, p);

                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = InternalGetString(m, Locales[defaultResource], p);
                    }
                }
                else
                {
                    output = InternalGetString(m, Locales[defaultResource], p);
                }

                return output;
            }
        }

        private bool InternalStringAvailable(string m, ResourceManager lang)
        {
            return lang.GetString(m) != null;
        }
        private string InternalGetString(string m, ResourceManager lang, params object[] p)
        {
            return (p.Length == 0) ? lang.GetString(m) : string.Format(lang.GetString(m), p); ; 
        }
    }

    // Constants
    public partial class Locale
    {
        public const string CommandGlobalProfileUserHeader = "miki_global_profile_user_header";

        public const string DisabledCommand = "miki_module_admin_disable_command";
        public const string DisabledModule = "miki_module_admin_disable_module";

        public const string EnabledCommand = "miki_module_admin_enable_command";
        public const string EnabledModule = "miki_module_admin_enable_module";

        public const string ErrorMessageGeneric = "miki_error_message_generic";
        public const string ErrorPickNoArgs = "miki_module_fun_pick_no_arg";

        public const string ImageNotFound = "miki_module_fun_image_error_no_image_found";

        public const string InsufficientMekos = "miki_mekos_insufficient";

        public const string JoinMessage = "miki_join_message";

        public const string PickMessage = "miki_module_fun_pick";

        public const string RollResult = "miki_module_fun_roll_result";

        public const string RouletteMessageNoArg = "miki_module_fun_roulette_winner_no_arg";
        public const string RouletteMessage = "miki_module_fun_roulette_winner";

        public const string SlotsHeader = "miki_module_fun_slots_header";

        public const string SlotsWinHeader = "miki_module_fun_slots_win_header";
        public const string SlotsWinMessage = "miki_module_fun_slots_win_amount";

        public const string SuccessMessageGeneric = "miki_success_message_generic";
    }
}
