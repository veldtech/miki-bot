namespace Miki.Services.Pasta.Exceptions
{
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class PastaNotFoundException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_pasta_null");
    }
}
