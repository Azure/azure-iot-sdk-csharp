// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    public class ProvisioningTransportException : Exception
    {
        const string IsTransientValueSerializationStoreName = "ProvisioningTransportException-IsTransient";

        public bool IsTransient { get; private set; }

        public string TrackingId { get; set; }

        public ProvisioningTransportException()
        {
        }
        
        public ProvisioningTransportException(string message, bool isTransient, string trackingId, Exception innerException)
            : this(message, innerException, isTransient, trackingId)
        {
        }

        public ProvisioningTransportException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        public ProvisioningTransportException(string message)
            :this(message, null, false)
        {

        }

        public ProvisioningTransportException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        protected ProvisioningTransportException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        protected ProvisioningTransportException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            this.IsTransient = isTransient;
            this.TrackingId = trackingId;
        }

        protected ProvisioningTransportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                this.IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                this.TrackingId = info.GetString(IsTransientValueSerializationStoreName);
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, this.IsTransient);
        }
    }
}
