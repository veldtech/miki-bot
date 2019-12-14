namespace Miki.Utility
{
    using System;
    using System.Reflection;
    using Miki.Attributes;
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework.Arguments;

    public static class ArgumentPackExtensions
    {
        public static T TakeRequired<T>(this ITypedArgumentPack pack)
        {
            if(pack.Take<T>(out T value))
            {
                return value;
            }

            Type t = typeof(T);
            string verb = null;

            if(t.IsClass)
            {
                var attr = t.GetCustomAttribute<VerbAttribute>();
                if(attr == null)
                {
                    throw new InvalidOperationException("Cannot apply verb to current type");
                }

                verb = attr.Value;
            }

            throw new ArgumentMissingException(verb);
        }

        private static string GetVerbFromBaseType(Type t)
        {
            if(t.IsAssignableFrom(typeof(string)))
            {
                return "verb_word";
            }

            if(t.IsAssignableFrom(typeof(int))
               || t.IsAssignableFrom(typeof(uint))
               || t.IsAssignableFrom(typeof(long))
               || t.IsAssignableFrom(typeof(ulong))
               || t.IsAssignableFrom(typeof(short))
               || t.IsAssignableFrom(typeof(ushort))
               || t.IsAssignableFrom(typeof(float))
               || t.IsAssignableFrom(typeof(double))
               || t.IsAssignableFrom(typeof(decimal)))
            {
                return "verb_number";
            }

            if(t.IsAssignableFrom(typeof(bool)))
            {
                return "verb_switch";
            }

            return "verb_object";
        }
    }
}
