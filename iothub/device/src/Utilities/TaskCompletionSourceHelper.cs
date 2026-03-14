// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Modern .NET supports waiting on the TaskCompletionSource with a cancellation token, but older ones
    /// do not. We can bind that task with a call to Task.Delay to get the same effect, though.
    /// </summary>
    internal static class TaskCompletionSourceHelper
    {
        internal static async Task<T> WaitAsync<T>(this TaskCompletionSource<T> taskCompletionSource, CancellationToken ct)
        {
#if NET6_0_OR_GREATER
            return await taskCompletionSource.Task.WaitAsync(ct).ConfigureAwait(false);
#else
            await Task
                .WhenAny(
                    taskCompletionSource.Task,
                    Task.Delay(-1, ct))
                .ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
            return await taskCompletionSource.Task.ConfigureAwait(false);
#endif
        }
    }
}
