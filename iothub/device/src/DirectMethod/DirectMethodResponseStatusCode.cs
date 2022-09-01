// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Status code for method response.
    /// </summary>
    public enum DirectMethodResponseStatusCode
    {
        /// <summary>
        /// Equivalent to HTTP status code 400: bad request.
        /// </summary>
        BadRequest = 400,

        /// <summary>
        /// Error code when MethodHandler does not return a valid JSON.
        /// </summary>
        UserCodeException = 500,

        /// <summary>
        /// Equivalent to HTTP status code 501: not implemented server error. It is used when a method call from the service
        /// specifies a method name not registered with the client for callback.
        /// </summary>
        MethodNotImplemented = 501,
    }
}
