namespace Miki.Modules.Admin.Exceptions
{
    using System;
    using System.Reflection;
    using Miki.Attributes;
    using Miki.Bot.Models.Attributes;
    using Miki.Localization.Exceptions;
    using Miki.Localization.Models;

    public class InvalidEntityException : LocalizedException
    {
        private readonly string entity;

        /// <inheritdoc />
        public override IResource LocaleResource
            => new LanguageResource("miki_error_entity_invalid", entity);

        public InvalidEntityException(string entityVerb)
        {
            if(string.IsNullOrEmpty(entityVerb))
            {
                throw new ArgumentNullException();
            }

            entity = entityVerb;
        }

        public static InvalidEntityException FromEntity<T>()
        {
            return new InvalidEntityException(
                typeof(T).GetCustomAttribute<VerbAttribute>()?.Value ?? "unknown");
        }
    }
}
