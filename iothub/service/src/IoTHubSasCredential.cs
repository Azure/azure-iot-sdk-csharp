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
        : AzureSasCredential
    {
        /// <summary>
        /// Creates an instance of <see cref="IotHubSasCredential"/>.
        /// </summary>
        /// <param name="signature">Shared access signature used to authenticate with IoT hub.</param>
        /// <param name="expiresOnUtc">The shared access signature expiry in UTC.</param>
        public IotHubSasCredential(string signature, DateTime expiresOnUtc) : base(signature)
        {
            ExpiresOnUtc = expiresOnUtc;
        }

        /// <summary>
        /// The shared access signature expiry in UTC.
        /// </summary>
        public DateTime ExpiresOnUtc { get; private set; }

        /// <summary>
        /// Updates the shared access signature. This is intended to be used when you've
        /// regenerated your shared access signature and want to update clients.
        /// </summary>
        /// <param name="signature">Shared access signature used to authenticate with IoT hub.</param>
        /// <param name="expiresOnUtc">The shared access signature expiry in UTC.</param>
        public void Update(string signature, DateTime expiresOnUtc)
        {
            Update(signature);
            ExpiresOnUtc = expiresOnUtc;
        }
    }
}

#endif
