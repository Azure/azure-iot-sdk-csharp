﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// A retry policy that denies all retry inquiries.
    /// </summary>
    public class IotHubClientNoRetry : IIotHubClientRetryPolicy
    {
        /// <summary>
        /// Create an instance of this class.
        /// </summary>
        /// <example>
        /// <code language="csharp">
        /// var noRetry = new IotHubClientNoRetry();
        /// 
        /// var clientOptions = new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
        /// {
        ///     RetryPolicy = noRetry,
        /// };
        /// </code>
        /// </example>
        public IotHubClientNoRetry()
        {
            if (Logging.IsEnabled)
                Logging.Info(
                    this,
                    $"NOTE: A no-retry policy has been enabled; the client will not perform any retries on error.",
                    nameof(IotHubClientNoRetry));
        }

        /// <inheritdoc/>
        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryDelay)
        {
            retryDelay = TimeSpan.Zero;
            return false;
        }
    }
}
