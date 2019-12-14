namespace Miki.Utility
{
    using System;
    using System.Threading.Tasks;
    
    public static class AsyncResult
    {
        public static AsyncResult<T> From<T>(Func<ValueTask<T>> func)
        {
            return new AsyncResult<T>(() => func().AsTask());
        }
        public static AsyncResult<T> From<T>(Func<Task<T>> func)
        {
            return new AsyncResult<T>(func);
        }
    }

    public class AsyncResult<T>
    {
        private readonly Func<Task<T>> task;
        private readonly AsyncResult<T> innerResult;

        public AsyncResult(Func<Task<T>> func)
        {
            this.task = func;
        }

        private AsyncResult(Func<Task<T>> task, AsyncResult<T> inner)
        {
            this.task = task;
            this.innerResult = inner;
        }

        public AsyncResult<T> OrElse(Func<Task<T>> result)
        {
            if(innerResult == null)
            {
                return new AsyncResult<T>(task, new AsyncResult<T>(result));
            }

            return innerResult.OrElse(result);
        }

        public async Task<T> UnwrapAsync()
        {
            try
            {
                return await task();
            }
            catch
            {
                if(innerResult != null)
                {
                    return await innerResult.UnwrapAsync();
                }
                throw;
            }
        }

        public static implicit operator Task<T>(AsyncResult<T> result) 
            => result.task();
        public static implicit operator AsyncResult<T>(Task<T> task) 
            => new AsyncResult<T>(() => task);
        public static implicit operator AsyncResult<T>(ValueTask<T> task) 
            => new AsyncResult<T>(task.AsTask);
    }

    public class AsyncResult<T1, T2> : AsyncResult<Tuple<T1, T2>>
    {
        /// <inheritdoc />
        public AsyncResult(Func<Task<Tuple<T1, T2>>> func)
            : base(func)
        { }
    }
}