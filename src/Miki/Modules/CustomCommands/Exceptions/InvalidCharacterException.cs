namespace Miki.Modules.CustomCommands.Exceptions
{
    using Miki.Localization;
    using Miki.Localization.Exceptions;

    /// <summary>
    /// Throws when a character is not supported in the current context.
    /// </summary>
    internal class InvalidCharacterException : LocalizedException
    {
        public override IResource LocaleResource 
            => new LanguageResource("error_invalid_character", _character);

        private readonly string _character;

        public InvalidCharacterException(string character)
        {
            _character = character;
        }
    }
}