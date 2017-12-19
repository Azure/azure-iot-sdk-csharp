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

        internal ProvisioningServiceClientHttpException(ContractApiResponse response, bool isTransient) 
            : base($"{response.ErrorMessage}:{response.Body}", isTransient: isTransient)
        {
            Body = response.Body;
            StatusCode = response.StatusCode;
            Fields = response.Fields;
            ErrorMessage = response.ErrorMessage;
        }
    }
}
