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
            => new LanguageResource("error_entity_invalid", GetEntityResource());

        public InvalidEntityException(Type t)
        {
            entity = t.GetCustomAttribute<EntityAttribute>()?.Value ?? "object";
        }
        public InvalidEntityException(string entityVerb)
        {
            if(string.IsNullOrEmpty(entityVerb))
            {
                throw new ArgumentNullException(entityVerb);
            }

            entity = entityVerb;
        }

        private IResource GetEntityResource()
        {
            return new LanguageResource($"entity_{entity}");
        }
    }
}
