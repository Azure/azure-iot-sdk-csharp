﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.TransientFaultHandling;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    internal class RetryStrategyAdapter : RetryStrategy
    {
        private readonly IRetryPolicy _retryStrategy;

        public RetryStrategyAdapter(IRetryPolicy retryPolicy)
            : base(null, false)
        {
            _retryStrategy = retryPolicy;
        }

        public override ShouldRetry GetShouldRetry()
        {
            return ShouldRetry;
        }

        private bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan retryInterval)
        {
            bool shouldRetry = false;

            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(this, retryCount, lastException, $"{nameof(RetryStrategyAdapter)}.{nameof(ShouldRetry)}");

                shouldRetry = _retryStrategy.ShouldRetry(retryCount, lastException, out retryInterval);
                Logging.Info(this, $"ShouldRetry = {shouldRetry}, after {retryInterval}");
                return shouldRetry;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"{nameof(RetryStrategyAdapter)}.{nameof(ShouldRetry)}");
            }
        }
    }
}
