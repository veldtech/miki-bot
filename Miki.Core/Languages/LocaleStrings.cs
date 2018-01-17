using IA;
using Miki.Core.Languages;
using Miki.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Resources;
using System.Threading.Tasks;

namespace Miki.Languages
{
    // Class
    public partial class Locale
    {
        public static Dictionary<string, ResourceManager> Locales = new Dictionary<string, ResourceManager>();
        public static Dictionary<string, string> LocaleNames = new Dictionary<string, string>();

		private static ConcurrentDictionary<long, string> cache = new ConcurrentDictionary<long, string>();

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
            LocaleNames.Add("arabic", "ar-ae");

            Locales.Add("cz-cz", cz_CZ.ResourceManager);
            LocaleNames.Add("czech", "cz-cz");

            Locales.Add("da-dk", da_DK.ResourceManager);
            LocaleNames.Add("danish", "da-dk");

            Locales.Add("de-de", de_DE.ResourceManager);
            LocaleNames.Add("german", "de-de");

            Locales.Add("en-us", en_US.ResourceManager);
            LocaleNames.Add("english", "en-us");

            Locales.Add("es-es", es_ES.ResourceManager);
            LocaleNames.Add("spanish", "es-es");

            Locales.Add("fi-fi", fi_FI.ResourceManager);
            LocaleNames.Add("finnish", "fi-fi");

            Locales.Add("fr-fr", fr_FR.ResourceManager);
            LocaleNames.Add("french", "fr-fr");

            Locales.Add("he-he", he_HE.ResourceManager);
            LocaleNames.Add("hebrew", "he-he");

            Locales.Add("ja-ja", ja_JA.ResourceManager);
            LocaleNames.Add("japanese", "ja-ja");

            Locales.Add("nl-nl", nl_NL.ResourceManager);
            LocaleNames.Add("dutch", "nl-nl");

            Locales.Add("no-no", no_NO.ResourceManager);
            LocaleNames.Add("norwegian", "no-no");

            Locales.Add("pt-pt", pt_PT.ResourceManager);
            LocaleNames.Add("portuguese", "pt-pt");

            Locales.Add("ru-ru", ru_RU.ResourceManager);
            LocaleNames.Add("russian", "ru-ru");

            Locales.Add("sv-se", sv_SE.ResourceManager);
            LocaleNames.Add("swedish", "sv-se");

            Locales.Add("zh-chs", zh_CHS.ResourceManager);
            LocaleNames.Add("simplified chinese", "zh-chs");

            Locales.Add("zh-cht", zh_CHT.ResourceManager);
            LocaleNames.Add("traditional chinese", "zh-cht");
        }

        public static Locale GetEntity(ulong id) => GetEntity(id.ToDbLong());
        public static Locale GetEntity(long id)
        {
            return new Locale(id);
        }

        public bool HasString(string m)
        {
			string lang = GetLanguage();
			string output = Locales[lang].GetString(m);

			if (string.IsNullOrWhiteSpace(output))
			{
				output = Locales[defaultResource].GetString(m);
			}

			return !string.IsNullOrWhiteSpace(output);
		}

        public string GetString(string m, params object[] p)
        {
			string language = GetLanguage();
			ResourceManager resources = Locales[language];
			string output = "";

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

		public string GetLanguage()
		{
			if (cache.TryGetValue(id, out string language))
			{
				return language;
			}
			else
			{
				using (var context = new MikiContext())
				{
					ChannelLanguage l = context.Languages.Find(id);
					if (l != null)
					{
						cache.TryAdd(id, l.Language);
						return l.Language;
					}
				}
			}
			return cache.GetOrAdd(id, defaultResource);
		}
		public static async Task SetLanguageAsync(long id, string language)
		{
			using (var context = new MikiContext())
			{
				ChannelLanguage lang = await context.Languages.FindAsync(id);
				Locale locale = GetEntity(id);

				if(LocaleNames.TryGetValue(language, out string val))
				{
					language = val;
				}

				if (lang == null)
				{
					lang = context.Languages.Add(new ChannelLanguage()
					{
						EntityId = id,
						Language = language
					}).Entity;
				}

				lang.Language = language;

				cache.AddOrUpdate(id, lang.Language, (x, y) => lang.Language);

				await context.SaveChangesAsync();
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