namespace Miki.Utility
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class Task<T1, T2> : Task<Tuple<T1, T2>> {
        /// <inheritdoc />
        public Task(Func<object, Tuple<T1, T2>> function, object state)
            : base(function, state)
        {
        }

        /// <inheritdoc />
        public Task(Func<object, Tuple<T1, T2>> function, object state, CancellationToken cancellationToken)
            : base(function, state, cancellationToken)
        {
        }

        /// <inheritdoc />
        public Task(Func<object, Tuple<T1, T2>> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : base(function, state, cancellationToken, creationOptions)
        {
        }

        /// <inheritdoc />
        public Task(Func<object, Tuple<T1, T2>> function, object state, TaskCreationOptions creationOptions)
            : base(function, state, creationOptions)
        {
        }

        /// <inheritdoc />
        public Task(Func<Tuple<T1, T2>> function)
            : base(function)
        {
        }

        /// <inheritdoc />
        public Task(Func<Tuple<T1, T2>> function, CancellationToken cancellationToken)
            : base(function, cancellationToken)
        {
        }

        /// <inheritdoc />
        public Task(Func<Tuple<T1, T2>> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions)
            : base(function, cancellationToken, creationOptions)
        {
        }

        /// <inheritdoc />
        public Task(Func<Tuple<T1, T2>> function, TaskCreationOptions creationOptions)
            : base(function, creationOptions)
        {
        }
    }

    public static class TaskExtensions
    {
        public static Task<Tuple<T1, T2>> Merge<T1, T2>(
            this Func<Task<T1>> result1, Func<ValueTask<T2>> result2)
        {
            return Merge(result1, () => result2().AsTask());
        }
        public static Task<Tuple<T1, T2>> Merge<T1, T2>(
            this ValueTask<T1> result1, Func<ValueTask<T2>> result2)
        {
            return Merge(result1.AsTask, () => result2().AsTask());
        }

        public static async Task<Tuple<T1, T2>> Merge<T1, T2>(
            Func<Task<T1>> task1,
            Func<Task<T2>> task2)
        {
            T1 t1 = await task1();
            T2 t2 = await task2();
            return new Tuple<T1, T2>(t1, t2);
        }

        public static async Task<TOut> Map<TIn, TOut>(
            this Task<TIn> result,
            Func<TIn, TOut> func)
        {
            ThrowOnFaultyTask(result);
            return func(await result);
        }

        public static Task<TOut> FlatMap<TIn, TOut>(
            this Task<Task<TIn>> result,
            Func<TIn, TOut> func)
        {
            ThrowOnFaultyTask(result);
            return result.Unwrap()
                .Map(func);
        }

        public static Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<Task> func)
        {
            return result.Map(x =>
            {
                func();
                return x;
            });
        }
        public static Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Action func)
        {
            return result.Map(x =>
            {
                func();
                return x;
            });
        }
        public static Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Action<TResult> func)
        {
            return result.Map(x =>
            {
                func(x);
                return x;
            });
        }
        public static Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<ValueTask> func)
        {
            return result.Map(x =>
            {
                func();
                return x;
            });
        }
        public static Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<TResult, Task> func)
        {
            return result.Map(x =>
            {
                func(x);
                return x;
            });
        }

        public static async Task<TResult> OrElse<TResult>(
            this Task<TResult> task,
            Func<Task<TResult>> otherTask)
        {
            try
            {
                return await task;
            }
            catch(TaskCanceledException)
            {
                throw;
            }
            catch(Exception)
            {
                return await otherTask();
            }
        }

        /// <summary>
        /// Unwraps the execution and gives a callback when an exception has been raised, but will still
        /// throw. This gives you some functionality to communicate that your logic has failed, but will
        /// not continue execution and give additional unrelated errors.
        ///
        /// To consume errors consider using <see cref="UnwrapErrorAsync{TResult}(Task{TResult}, Func{Exception, Task{TResult}})"/>
        /// </summary>
        /// <returns></returns>
        public static async Task<TResult> UnwrapErrorAsync<TResult>(
            this Task<TResult> result,
            Func<Exception, Task> func)
        {
            try
            {
                return await result;
            }
            catch(Exception e)
            {
                await func(e);
                throw;
            }
        }

        /// <summary>
        /// Unwraps the execution like normally, but allows for a failover function whenever the
        /// execution fails. Passing back a correct value will allow for the 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static async Task<TResult> UnwrapErrorAsync<TResult>(
            this Task<TResult> result,
            Func<Exception, Task<TResult>> func)
        {
            try
            {
                return await result;
            }
            catch(Exception e)
            {
                return await func(e);
            }
        }

        private static void ThrowOnFaultyTask(Task t)
        {
            if(t.IsFaulted)
            {
                throw t.Exception;
            }
        }
    }
}