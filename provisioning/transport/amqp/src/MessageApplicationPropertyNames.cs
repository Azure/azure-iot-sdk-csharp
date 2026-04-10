// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal static class MessageApplicationPropertyNames
    {
        public const string Prefix = "iotdps-";
        public const string OperationType = Prefix + "operation-type";
        public const string OperationId = Prefix + "operation-id";
        public const string Status = Prefix + "status";
        public const string StatusCode = "statusCode"; // Is this needed?
        public const string ForceRegistration = Prefix + "forceRegistration";
    }
}
