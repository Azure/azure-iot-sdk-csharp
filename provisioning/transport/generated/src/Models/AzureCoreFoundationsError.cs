// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> The error object. </summary>
    public class AzureCoreFoundationsError
    {
        /// <summary> Initializes a new instance of AzureCoreFoundationsError. </summary>
        /// <param name="code"> One of a server-defined set of error codes. </param>
        /// <param name="message"> A human-readable representation of the error. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="code"/> or <paramref name="message"/> is null. </exception>
        internal AzureCoreFoundationsError(string code, string message)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            Code = code;
            Message = message;
            Details = new List<AzureCoreFoundationsError>();
        }

        /// <summary> Initializes a new instance of AzureCoreFoundationsError. </summary>
        /// <param name="code"> One of a server-defined set of error codes. </param>
        /// <param name="message"> A human-readable representation of the error. </param>
        /// <param name="target"> The target of the error. </param>
        /// <param name="details"> An array of details about specific errors that led to this reported error. </param>
        /// <param name="innererror"> An object containing more specific information than the current object about the error. </param>
        internal AzureCoreFoundationsError(string code, string message, string target, IReadOnlyList<AzureCoreFoundationsError> details, AzureCoreFoundationsInnerError innererror)
        {
            Code = code;
            Message = message;
            Target = target;
            Details = details;
            Innererror = innererror;
        }

        /// <summary> One of a server-defined set of error codes. </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; }
        /// <summary> A human-readable representation of the error. </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; }
        /// <summary> The target of the error. </summary>
        [JsonProperty(PropertyName = "target")]
        public string Target { get; }
        /// <summary> An array of details about specific errors that led to this reported error. </summary>
        [JsonProperty(PropertyName = "details")]
        public IReadOnlyList<AzureCoreFoundationsError> Details { get; }
        /// <summary> An object containing more specific information than the current object about the error. </summary>
        [JsonProperty(PropertyName = "innererror")]
        public AzureCoreFoundationsInnerError Innererror { get; }
    }
}
