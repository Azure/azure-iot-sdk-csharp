using System;
using System.Linq;
using System.Threading.Tasks;

namespace DeviceExplorer
{
    internal static class ThreadUtil
    {
        /// <summary>
        ///     Fire off a task that can complete whenever it pleases.  This method
        ///     is mostly to improve the semantics of calling an async method from
        ///     a synchronous method when we don't care about the exact time table
        ///     or return value.
        /// </summary>
        /// <remarks>
        ///     Copied from https://stackoverflow.com/a/22864616/2517147
        /// </remarks>
        /// <param name="task"></param>
        /// <param name="acceptableExceptions"></param>
        public static async void FireAndForget(
            this Task task,
            params Type[] acceptableExceptions)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // TODO: consider whether derived types are also acceptable.
                if (!acceptableExceptions.Contains(ex.GetType()))
                    throw;
            }
        }
    }
}