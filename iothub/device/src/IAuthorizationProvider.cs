// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETMF
using System.Threading.Tasks;
#endif

namespace Microsoft.Azure.Devices.Client
{
    interface IAuthorizationProvider
    {
#if !NETMF
        Task<string> GetPasswordAsync();
#else
        string GetPassword();
#endif
    }
}
