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
    /// <remarks>
    /// This exception identify that the provisioning service respond the HTTP request with a error status code.
    /// <code>
    /// ProvisioningServiceClientHttpException [any exception reported in the HTTP response]
    ///     \ \ \ \__StatusCode [the returned HTTP status code]
    ///      \ \ \___ErrorMessage [the root cause of the error]
    ///       \ \____Body [the HTTP message body with details about the error]
    ///        \_____Filds [the HTTP head fields that may provide more details about the error]
    ///
    /// </code>
    /// </remarks>
    public class ProvisioningServiceClientHttpException : ProvisioningServiceClientTransportException
    {
        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientHttpException"/> class.
        /// </summary>
        public ProvisioningServiceClientHttpException()
            : base()
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientHttpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ProvisioningServiceClientHttpException(string message)
            : base(message: message)
        {
        }

        /// <summary>
        /// Initializes the <see cref="ProvisioningServiceClientHttpException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProvisioningServiceClientHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal ProvisioningServiceClientHttpException(ContractApiResponse response, bool isTransient)
            : base($"{response.ErrorMessage}:{response.Body}", isTransient: isTransient)
        {
            Body = response.Body;
            StatusCode = response.StatusCode;
            Fields = response.Fields;
            ErrorMessage = response.ErrorMessage;
        }

        /// <summary>
        /// The HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// The HTTP body.
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// The HTTP headers.
        /// </summary>
        public IDictionary<string, string> Fields { get; private set; }

        /// <summary>
        /// The error message.
        /// </summary>
        public string ErrorMessage { get; private set; }
    }
}
