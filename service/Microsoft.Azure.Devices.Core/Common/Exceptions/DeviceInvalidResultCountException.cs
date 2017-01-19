// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class DeviceInvalidResultCountException : IotHubException
    {
        public DeviceInvalidResultCountException(int maximumResultCount)
            : base("Number of device results must be between 0 and {0}".FormatInvariant(maximumResultCount))
        {
            this.MaximumResultCount = maximumResultCount;
        }

        internal int MaximumResultCount
        {
            get;
            private set;
        }
    }
}
