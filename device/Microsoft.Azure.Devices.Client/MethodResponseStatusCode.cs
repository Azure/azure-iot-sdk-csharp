// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Status code for Method Response
    /// </summary>
    public enum MethodResposeStatusCode
    {
        BadRequest = 400,
        UserCodeException = 500,
        MethodNotImplemented = 501
    }
}
