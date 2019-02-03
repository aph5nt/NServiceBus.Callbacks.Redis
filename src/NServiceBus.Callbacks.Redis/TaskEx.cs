namespace NServiceBus.Callbacks.Redis
{
    using System;
    using System.Threading.Tasks;

    public static class TaskEx
    {
        /// <summary>
        /// Adds a timeout to the task.
        /// </summary>
        /// <typeparam name="TResult">Targtet task return type.</typeparam>
        /// <param name="task">Target task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            // if the original task finishes first, then we're good
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
                return await task;

            // or else that means the timeout finished first, in which case throw
            throw new TimeoutException();
        }

        /// <summary>
        /// Adds a timeout to the task.
        /// </summary>
        /// <param name="task">Target task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
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
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25)
        {
            while (condition()) await Task.Delay(frequency);
        }
    }
}
