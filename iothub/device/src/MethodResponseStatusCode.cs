// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Status code for Method Response
    /// </summary>
    public enum MethodResponseStatusCode
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        BadRequest = 400,

        /// <summary>
        /// Error code when MethodHandler does not return a valid json.
        /// </summary>
        UserCodeException = 500,

        MethodNotImplemented = 501,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
