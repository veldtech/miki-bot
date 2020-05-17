namespace Miki
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    internal class LocalExperienceNullException : LocalizedException
	{
		public override IResource LocaleResource
			=> new LanguageResource("error_local_experience_null");
	}
}