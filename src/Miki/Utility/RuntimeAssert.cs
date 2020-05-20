using System;
using Miki.Localization.Exceptions;

namespace Miki.Utility
{
    public static class RuntimeAssert
    {
        public static void NotNull<T>(T any, LocalizedException exception = null)
        {
            if(!(any?.Equals(default(T)) ?? true))
            {
                return;
            }

            if(exception != null)
            {
                throw exception;
            }

            throw new InvalidOperationException(
                $"Object of type '{typeof(T).Name}' equals to null");
        }
    }
}
