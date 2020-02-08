namespace Miki.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Result<T>
    {
        private readonly T value;
        private readonly Exception exception;

        public bool IsValid => exception == null;

        public Result(T value)
            : this(value, null)
        {}

        public Result(Exception exception)
            : this(default, exception)
        {}

        public Result(Func<T> expression)
        {
            try
            {
                value = expression();
            }
            catch(Exception e)
            {
                exception = e;
            } 
        }

        protected Result(T value, Exception exception)
        {
            this.value = value;
            this.exception = exception;
        }

        public Result<T> OrElse(Result<T> result)
        {
            if(IsValid)
            {
                return this;
            }
            return result;
        }

        public virtual T Unwrap()
        {
            if(IsValid)
            {
                return value;
            }
            throw exception;
        }

        public virtual Exception UnwrapException()
        {
            if(IsValid)
            {
                throw new InvalidOperationException("Tried to unwrap exception, but state was valid.");
            }

            return exception;
        }
    }
}
