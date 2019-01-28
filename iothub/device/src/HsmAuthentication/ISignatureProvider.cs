// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// HSM signature provider.
    /// </summary>
    internal interface ISignatureProvider
    {
        Task<string> SignAsync(string moduleId, string generationId, string data);
    }
}
