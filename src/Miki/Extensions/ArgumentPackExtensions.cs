namespace Miki
{
    using Miki.Bot.Models.Exceptions;
    using Miki.Framework.Arguments;

    public static class ArgumentPackExtensions
    {
        public static T TakeRequired<T>(this ITypedArgumentPack pack)
        {
            if(pack.Take(out T value))
            {
                return value;
            }
            throw new ArgumentMissingException(typeof(T));
        }
    }
}
