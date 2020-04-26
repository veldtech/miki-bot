namespace Miki
{
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework.Arguments;
    using Miki.Functional;

    public static class ArgumentPackExtensions
    {
        public static Result<T> Take<T>(this ITypedArgumentPack pack)
        {
            if(pack.Take(out T value))
            {
                return value;
            }
            throw new ArgumentMissingException(typeof(T));
        }

        public static Result<T> Take<T>(this ITypedArgumentPack pack, string noun)
        {
            if(pack.Take(out T value))
            {
                return value;
            }
            return new ArgumentMissingException(noun);
        }

        public static T TakeRequired<T>(this ITypedArgumentPack pack)
        {
            if(pack.Take(out T value))
            {
                return value;
            }
            throw new ArgumentMissingException(typeof(T));
        }

        public static T TakeRequired<T>(this ITypedArgumentPack pack, string noun)
        {
            if(pack.Take(out T value))
            {
                return value;
            }
            throw new ArgumentMissingException(noun);
        }
    }
}
