﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Attestation using a symmetric key.
    /// </summary>
    public sealed class SymmetricKeyAttestation : Attestation
    {
        /// <summary>
        /// Creates an instance of this class without specifying keys to let the service generate them.
        /// </summary>
        public SymmetricKeyAttestation()
        { }

        /// <summary>
        /// Creates an instance of this class with the specified keys.
        /// </summary>
        /// <param name="primaryKey">The primary key to use for attestation.</param>
        /// <param name="secondaryKey">The secondary key to use for attestation.</param>
        public SymmetricKeyAttestation(string primaryKey, string secondaryKey)
        {
            PrimaryKey = primaryKey;
            SecondaryKey = secondaryKey;
        }

        /// <summary>
        /// Gets the primary key used for attestation.
        /// </summary>
        [JsonProperty("primaryKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PrimaryKey { get; }

        /// <summary>
        /// Gets the secondary key used for attestation.
        /// </summary>
        [JsonProperty("secondaryKey", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SecondaryKey { get; }
    }
}