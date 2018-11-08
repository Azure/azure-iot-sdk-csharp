// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class ClientApiVersionHelper
    {
        // TODO: Split ApiVersionName as it is only used by HTTP and AMQP.
        public const string ApiVersionName = "api-version";
        public const string ApiVersion = "2017-11-15";
    }
}
