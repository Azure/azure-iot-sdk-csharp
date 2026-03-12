// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Options that allow configuration of the provisioning service client instance during initialization.
    /// </summary>
    public sealed class ProvisioningServiceClientOptions
    {
        /// <summary>
        /// The transport settings to use.
        /// </summary>
        public ProvisioningServiceHttpSettings ProvisioningServiceHttpSettings { get; } = new();

        /// <summary>
        /// The HTTP client to use for all HTTP operations.
        /// </summary>
        /// <remarks>
        /// If not provided, an HTTP client will be created for you based on the other settings provided.
        /// <para>
        /// If provided, all other HTTP-specific settings in <see cref="ProvisioningServiceHttpSettings"/> will be ignored
        /// and must be specified on the custom HttpClient instance.
        /// </para>
        /// </remarks>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <remarks>
        /// Defaults to a nearly infinite exponential backoff. If set to null, will use <see cref="ProvisioningServiceNoRetry"/> to perform no retries.
        /// Can be set to any of the built in retry policies such as <see cref="ProvisioningServiceFixedDelayRetryPolicy"/>
        /// or <see cref="ProvisioningServiceIncrementalDelayRetryPolicy"/> or a custom one by inheriting from <see cref="IProvisioningServiceRetryPolicy"/>.
        /// </remarks>
        public IProvisioningServiceRetryPolicy RetryPolicy { get; set; } = new ProvisioningServiceExponentialBackoffRetryPolicy(0, TimeSpan.FromHours(12), true);

        internal ProvisioningServiceClientOptions Clone()
        {
            return new ProvisioningServiceClientOptions
            {
                HttpClient = HttpClient,
                RetryPolicy = RetryPolicy,
            };
        }
    }
}
