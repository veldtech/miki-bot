namespace Miki.Utility
{
    using System;
    using Miki.Modules.Admin.Exceptions;
    
    /// <summary>
    /// Represents a static pattern to create implicit null checks. Used to improve business logic for
    /// modules.
    /// </summary>
    public struct Optional<T>
    {
        private readonly T value;

        /// <summary>
        /// Creates an immutable optional object. 
        ///
        /// Keep in mind that if you want your object back, you have to <see cref="Optional{T}.Unwrap"/>
        /// or <see cref="Optional{T}.UnwrapDefault(T)"/>. 
        /// </summary>
        public Optional(T value)
        {
            this.value = value;
        }

        public bool HasValue => !CheckIfValueNull();

        public T Unwrap()
        {
            return HasValue
                ? value
                : throw InvalidEntityException.FromEntity<T>();
        }

        public T UnwrapDefault(T defaultValue = default)
        {
            return HasValue
                ? value
                : defaultValue;
        }

        private bool CheckIfValueNull()
        {
            if(typeof(T).IsValueType)
            {
                return false;
            }
            return (object)value == default;
        }

        public static implicit operator Optional<T>(T value) 
            => new Optional<T>(value);

        public static implicit operator T(Optional<T> value) 
            => value.Unwrap();
        public static Optional<T> None 
            => new Optional<T>(default);
    }
}
