// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on an edge device or an edge module by an <see cref="IotHubModuleClient"/>.
    /// </summary>
    public class EdgeModuleDirectMethodRequest : DirectMethodRequest
    {
        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        public EdgeModuleDirectMethodRequest(string methodName)
            : this(methodName, null)
        {
        }

        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        /// <param name="payload">The direct method payload bytes.</param>
        public EdgeModuleDirectMethodRequest(string methodName, byte[] payload)
            : base(methodName)
        {
            Payload = payload;
        }

        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        /// <param name="payload">The direct method payload that will be serialized and encoded per <see cref="IotHubClientOptions.PayloadConvention"/>.</param>
        public EdgeModuleDirectMethodRequest(string methodName, object payload)
            : base(methodName)
        {
            Payload = payload == null
                ? null
                : PayloadConvention.GetObjectBytes(payload);
        }
    }
}
