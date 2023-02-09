// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    internal sealed class ContractApiResponse
    {
        internal ContractApiResponse(
            string body,
            HttpStatusCode statusCode,
            IDictionary<string, string> fields,
            string errorMessage)
        {
            Body = body;
            StatusCode = statusCode;
            Fields = fields;
            ErrorMessage = errorMessage;
        }

        public HttpStatusCode StatusCode { get; private set; }
        public string Body { get; private set; }
        public IDictionary<string, string> Fields { get; private set; }
        public string ErrorMessage { get; private set; }
    }
}
