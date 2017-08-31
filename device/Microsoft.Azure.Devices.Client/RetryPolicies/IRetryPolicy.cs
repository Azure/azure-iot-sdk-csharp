// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;

    /// <summary>
    /// Represents a retry policy
    /// </summary>
    public interface IRetryPolicy
    {
        bool ShouldRetry(int currentRetryCount, Exception lastException, out TimeSpan retryInterval);
    }
}
