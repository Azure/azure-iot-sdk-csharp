// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

#if !NET451

using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Shared access signature credential used to authenticate with IoT hub.
    /// </summary>
    public class IotHubSasCredential
    {
        /// <summary>
        /// Creates an instance of <see cref="IotHubSasCredential"/>.
        /// </summary>
        /// <param name="signature">Shared access signature used to authenticate with IoT hub.</param>
        /// <param name="expiresOn">The shared access signature expiry.</param>
        public IotHubSasCredential(string signature, DateTimeOffset expiresOn)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                throw new ArgumentNullException(nameof(signature), "Parameter cannot be null or empty");
            }

            if (expiresOn == null)
            {
                throw new ArgumentNullException(nameof(expiresOn), "Parameter cannot be null");
            }

            SasCredential = new AzureSasCredential(signature);
            ExpiresOn = expiresOn;
        }

        /// <summary>
        /// The shared access signature expiry.
        /// </summary>
        public DateTimeOffset ExpiresOn { get; private set; }

        /// <summary>
        /// The shared access signature credential used to authenticate with IoT hub.
        /// </summary>
        public AzureSasCredential SasCredential { get; private set; }

        /// <summary>
        /// Updates the shared access signature. This is intended to be used when you've
        /// regenerated your shared access signature and want to update clients.
        /// </summary>
        /// <param name="signature">Shared access signature used to authenticate with IoT hub.</param>
        /// <param name="expiresOn">The shared access signature expiry.</param>
        public void Update(string signature, DateTimeOffset expiresOn)
        {
            SasCredential.Update(signature);
            ExpiresOn = expiresOn;
        }
    }
}

#endif
