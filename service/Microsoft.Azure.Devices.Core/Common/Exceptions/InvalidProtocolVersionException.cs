// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public class InvalidProtocolVersionException : IotHubException
    {
        public InvalidProtocolVersionException(string version)
            : base(!string.IsNullOrEmpty(version) ? "Invalid protocol version: " + version : "Protocol version is required. But, it was not provided")
        {
            this.RequestedVersion = version;
        }
        public string RequestedVersion { get; private set; }
    }
}
