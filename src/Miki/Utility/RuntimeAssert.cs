namespace Miki.Utility
{
    using System;
    using Miki.Localization.Exceptions;

    public static class RuntimeAssert
    {
        public static void NotNull<T>(T any, LocalizedException exception = null)
        {
            if(any?.Equals(default(T)) ?? true)
            {
                if(exception != null)
                {
                    throw exception;
                }

                throw new InvalidOperationException(
                    $"Object of type '{typeof(T).Name}' equals to null");
            }
        }
    }
}
