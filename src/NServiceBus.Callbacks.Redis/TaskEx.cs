using System;
using System.Threading.Tasks;

namespace NServiceBus.Callbacks.Redis
{
    public static class TaskEx
    {
        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            // if the original task finishes first, then we're good
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
                return await task;

            // or else that means the timeout finished first, in which case throw
            throw new TimeoutException();
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            // if the original task finishes first, then we're good
            if (task != await Task.WhenAny(task, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)))
                throw new TimeoutException();
        }
    }
}
