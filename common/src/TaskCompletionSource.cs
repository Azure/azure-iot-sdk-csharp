// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// An implementation of <c>TaskCompletionSource&lt;bool&gt;</c> that returns a <c>Task</c> when completed.
    /// </summary>
    /// <remarks>
    /// Represents the producer side of a <c>Task</c> to a delegate, providing access to the consumer side through the <c>Task</c> property.
    /// This is used for .NET implementations lower than .NET 5.0, which lack a native implementation of the non-generic TaskCompletionSource.
    /// </remarks>
    internal sealed class TaskCompletionSource : TaskCompletionSource<bool>
    {
        public bool TrySetResult() => TrySetResult(true);

        public void SetResult() => SetResult(true);

        public override string ToString() => $"TaskCompletionSource[status: {Task.Status}]";
    }
}