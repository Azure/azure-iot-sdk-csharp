// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class HubServiceTestRetryPolicy : IIotHubServiceRetryPolicy
    {
        private readonly HashSet<IotHubServiceErrorCode> _iotHubServiceErrorCodes;
        private const int MaxRetries = 20;
        private static readonly TimeSpan s_retryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_maxDelay = TimeSpan.FromSeconds(3);

        public HubServiceTestRetryPolicy(HashSet<IotHubServiceErrorCode> iotHubServiceErrorCodes = default)
        {
            _iotHubServiceErrorCodes = iotHubServiceErrorCodes ?? new HashSet<IotHubServiceErrorCode>();
        }

        public bool ShouldRetry(uint currentRetryCount, Exception lastException, out TimeSpan retryInterval)
        {
            retryInterval = TimeSpan.Zero;

            if (currentRetryCount < MaxRetries)
            {
                VerboseTestLogger.WriteLine($"{nameof(HubServiceTestRetryPolicy)}: Exhausted {currentRetryCount}/{MaxRetries} retries and failing due to {lastException}");
                return false;
            }

            if (lastException is not IotHubServiceException)
            {
                VerboseTestLogger.WriteLine($"{nameof(HubServiceTestRetryPolicy)}: Unretriable exception encountered: {lastException}");
                return false;
            }

            var hubEx = (IotHubServiceException)lastException;

            if (hubEx.IsTransient
                || _iotHubServiceErrorCodes.Contains(hubEx.ErrorCode))
            {
                VerboseTestLogger.WriteLine($"{nameof(HubServiceTestRetryPolicy)}: retrying due to transient {hubEx.IsTransient} or error code {hubEx.ErrorCode}.");
                double waitDurationMs = Math.Min(
                    currentRetryCount * s_retryDelay.TotalMilliseconds,
                    s_maxDelay.TotalMilliseconds);

                retryInterval = TimeSpan.FromMilliseconds(waitDurationMs);

                return true;
            }

            VerboseTestLogger.WriteLine($"{nameof(HubServiceTestRetryPolicy)}: not retrying due to transient {hubEx.IsTransient} or error code {hubEx.ErrorCode}.");
            return false;
        }
    }
}
