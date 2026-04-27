// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Symmetric Key registration result.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Used by the JSon parser.")]
    internal partial class SymmetricKeyRegistrationResult
    {
        /// <summary>
        /// Initializes a new instance of the TpmRegistrationResult class.
        /// </summary>
        public SymmetricKeyRegistrationResult()
        {
            CustomInit();
        } 

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();
    }
}