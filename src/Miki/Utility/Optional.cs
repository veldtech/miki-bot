namespace Miki.Utility
{
    using System.Threading.Tasks;
    using Miki.Modules.Admin.Exceptions;

    public static class Optional
    {
        public static Optional<T> None<T>()
        {
            return new Optional<T>(default);
        }
    }

    /// <summary>
    /// Represents a static pattern to create implicit null checks. Used to improve business logic for
    /// modules.
    /// </summary>
    public struct Optional<T>
    {
        private readonly T value;

        /// <summary>
        /// Creates a immutable optional object. 
        ///
        /// Keep in mind that if you want your object back, you
        /// have to <see cref="Optional{T}.Unwrap"/> or <see cref="Optional{T}.UnwrapDefault(T)"/>. 
        /// </summary>
        public Optional(T value)
        {
            this.value = value;
        }

        public bool HasValue 
            => !value.Equals(default);

        public T Unwrap()
        {
            return HasValue
                ? value
                : throw InvalidEntityException.FromEntity<T>();

        }

        public T UnwrapDefault(T defaultValue = default)
        {
            return HasValue
                ? defaultValue
                : value;
        }

        public static implicit operator Optional<T>(T value) 
            => new Optional<T>(value);

        public static implicit operator T(Optional<T> value) 
            => value.Unwrap();

        public static Optional<T> None 
            => new Optional<T>(default);
    }

    public static class OptionalExtensions
    {
        public static async Task<Optional<T>> AsOptional<T>(this Task<T> task)
        {
            return new Optional<T>(await task);
        }
        public static async ValueTask<Optional<T>> AsOptional<T>(this ValueTask<T> task)
        {
            return new Optional<T>(await task);
        }
        public static Optional<T> AsOptional<T>(this T? nullable) 
            where T : struct
        {
            return new Optional<T>(nullable.GetValueOrDefault(default));
        }
    }
}
