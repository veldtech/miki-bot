namespace Miki.Modules.Admin.Exceptions
{
    using System;
    using System.Reflection;
    using Miki.Bot.Models.Attributes;
    using Miki.Localization;
    using Miki.Localization.Exceptions;

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
                throw new ArgumentNullException(entityVerb);
            }

            entity = entityVerb;
        }

        public static InvalidEntityException FromEntity<T>()
        {
            return new InvalidEntityException(
                typeof(T).GetCustomAttribute<EntityAttribute>()?.Value ?? "object");
        }
    }
}
