using System;
using System.Threading;
using System.Threading.Tasks;

namespace Miki.Utility
{
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
        public static async Task<Tuple<T1, T2>> Merge<T1, T2>(
            this Func<Task<T1>> task1, Func<ValueTask<T2>> task2)
        {
            T1 t1 = await task1();
            T2 t2 = await task2();
            return new Tuple<T1, T2>(t1, t2);
        }
        public static async Task<Tuple<T1, T2>> Merge<T1, T2>(
            this ValueTask<T1> result1, ValueTask<T2> result2)
        {
            T1 t1 = await result1;
            T2 t2 = await result2;
            return new Tuple<T1, T2>(t1, t2);
        }

        public static async Task<Tuple<T1, T2>> Merge<T1, T2>(
            Func<Task<T1>> task1,
            Func<Task<T2>> task2)
        {
            T1 t1 = await task1();
            T2 t2 = await task2();
            return new Tuple<T1, T2>(t1, t2);
        }

        public static async Task<TOut> Map<TOut>(
            this Task result,
            Func<Task<TOut>> func)
        {
            ThrowOnFaultyTask(result);
            await result;
            return await func();
        }

        public static async Task<TOut> Map<TIn, TOut>(
            this Task<TIn> result,
            Func<TIn, TOut> func)
        {
            ThrowOnFaultyTask(result);
            return func(await result);
        }
        public static async Task<TOut> Map<TIn, TOut>(
            this Task<TIn> result,
            Func<TIn, Task<TOut>> func)
        {
            ThrowOnFaultyTask(result);
            return await func(await result);
        }

        public static async Task<TOut> FlatMap<TIn, TOut>(
            this Task<Task<TIn>> task,
            Func<TIn, TOut> func)
        {
            ThrowOnFaultyTask(task);
            var innerTask = await task;
            var result = await innerTask;
            return func(result);
        }

        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<Task> func)
        {
            var x = await result;
            await func();
            return x;
        }
        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Action func)
        {
            var x = await result;
            func();
            return x;
        }
        public static async Task<TResult> AndThen<TResult>(
            this ValueTask<TResult> result,
            Action<TResult> func)
        {
            TResult t = await result;
            func(t);
            return t;
        }
        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Action<TResult> func)
        {
            var x = await result;
            func(x);
            return x;
        }
        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<ValueTask> func)
        {
            await func();
            return await result;
        }
        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<TResult, ValueTask> func)
        {
            var x = await result;
            await func(x);
            return x;
        }
        public static async Task<TResult> AndThen<TResult>(
            this Task<TResult> result,
            Func<TResult, Task> func)
        {
            var x = await result;
            await func(x);
            return x;
        }

        public static async Task<TResult> AndThen<TResult>(
            this ValueTask<TResult> result,
            Func<TResult, Task> func)
        {
            var r = await result;
            await func(r);
            return r;
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

        public static async Task<TResult> OrElseThrow<TResult>(
            this Task<TResult> task,
            Exception exception)
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
                throw exception;
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