// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Super class for the Device Provisioning Service exceptions on the Service Client.
    /// </summary>
    /// <remarks>
    /// <code>
    /// ProvisioningServiceClientException
    ///    |
    ///    +-->ProvisioningServiceClientTransportException [any transport layer exception]
    ///    |
    ///    +-->ProvisioningServiceClientServiceException [any exception reported in the http response]
    ///            |
    ///            |
    ///            +-->ProvisioningServiceClientBadUsageException [any http response 4xx]
    ///            |        |
    ///            |        +-->ProvisioningServiceClientBadFormatException [400]
    ///            |        +-->ProvisioningServiceClientUnathorizedException [401]
    ///            |        +-->ProvisioningServiceClientNotFoundException [404]
    ///            |        +-->ProvisioningServiceClientPreconditionFailedException [412]
    ///            |        +-->ProvisioningServiceClientTooManyRequestsException [429]
    ///            |
    ///            +-->ProvisioningServiceClientTransientException [any http response 5xx]
    ///            |        |
    ///            |        +-->ProvisioningServiceClientInternalServerErrorException [500]
    ///            |
    ///            +-->ProvisioningServiceClientUnknownException [any other http response >300, but not 4xx or 5xx]
    /// </code>
    /// </remarks>
#if !WINDOWS_UWP
    [Serializable]
#endif
    public class ProvisioningServiceClientException : Exception
    {
        private const string IsTransientValueSerializationStoreName = "ProvisioningException-IsTransient";
        private const string TrackingIdSerializationStoreName = "ProvisioningException-TrackingId";
        private const string ErrorCodeName = "ErrorCode";

        public bool IsTransient { get; private set; }

        public string TrackingId { get; set; }

        public ProvisioningServiceClientException(string message)
            : this(message, false)
        {
        }

        public ProvisioningServiceClientException(string message, string trackingId)
            : this(message, false, trackingId)
        {
        }

        public ProvisioningServiceClientException(string message, bool isTransient, string trackingId)
            : this(message, null, isTransient, trackingId)
        {
        }

        public ProvisioningServiceClientException(string message, bool isTransient)
            : this(message, null, isTransient, trackingId: string.Empty)
        {
        }

        public ProvisioningServiceClientException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public ProvisioningServiceClientException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        protected ProvisioningServiceClientException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        protected ProvisioningServiceClientException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            this.IsTransient = isTransient;
            this.TrackingId = trackingId;
        }

        //#if !WINDOWS_UWP && !NETSTANDARD1_3
        //        protected ProvisioningServiceClientException(SerializationInfo info, StreamingContext context)
        //            : base(info, context)
        //        {
        //            if (info != null)
        //            {
        //                this.IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
        //                this.TrackingId = info.GetString(TrackingIdSerializationStoreName);
        //            }
        //        }

        //        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //        {
        //            base.GetObjectData(info, context);
        //            info.AddValue(IsTransientValueSerializationStoreName, this.IsTransient);
        //            info.AddValue(TrackingIdSerializationStoreName, this.TrackingId);
        //        }
        //#endif
        //public ErrorCode Code
        //{
        //    get;
        //    private set;
        //}
    }
}
