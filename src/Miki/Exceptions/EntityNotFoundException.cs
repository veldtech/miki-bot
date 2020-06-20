using Miki.Bot.Models.Attributes;
using Miki.Localization;
using Miki.Localization.Exceptions;
using System.Reflection;
using System.Linq;

namespace Miki.Exceptions
{
    public class EntityNotFoundException<T> : LocalizedException
    {
        public override IResource LocaleResource
            => new LanguageResource("error_entity_not_found", GetEntityResource());

        private LanguageResource GetEntityResource()
        {
            var entityAttribute = typeof(T).GetCustomAttributes<EntityAttribute>(false)
                .FirstOrDefault();
            if (entityAttribute == null)
            {
                return new LanguageResource("entity_object");
            }

            return new LanguageResource(entityAttribute.Value);
        }
    }
}
