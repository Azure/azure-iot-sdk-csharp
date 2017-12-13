// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This is the subset of the Device Provisioning Service exceptions for the exceptions reported by the Service. 
    /// </summary>
    public class ProvisioningServiceClientHttpException : ProvisioningServiceClientException
    {
        public HttpStatusCode StatusCode { get; private set; }
        public string Body { get; private set; }
        public IDictionary<string, string> Fields { get; private set; }
        public string ErrorMessage { get; private set; }

        public ProvisioningServiceClientHttpException(string registrationId)
            : base(registrationId, string.Empty) { }

        public ProvisioningServiceClientHttpException(string registrationId, string trackingId)
            : base("Device Provisioning Service error for " + registrationId, trackingId) { }

        public ProvisioningServiceClientHttpException(string message, Exception innerException)
            : base(message, innerException) { }

        internal ProvisioningServiceClientHttpException(ContractApiResponse response) 
            : base(response.ErrorMessage)
        {
            Body = response.Body;
            StatusCode = response.StatusCode;
            Fields = response.Fields;
            ErrorMessage = response.ErrorMessage;
        }
    }
}
