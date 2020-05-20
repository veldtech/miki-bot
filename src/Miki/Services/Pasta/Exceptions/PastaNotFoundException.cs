using Miki.Localization.Exceptions;
using Miki.Localization;

namespace Miki.Services.Pasta.Exceptions
{
    public class PastaNotFoundException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_pasta_null");
    }
}
