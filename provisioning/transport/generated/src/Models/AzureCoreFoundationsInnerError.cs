// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary> An object containing more specific information about the error. As per Microsoft One API guidelines - https://github.com/Microsoft/api-guidelines/blob/vNext/Guidelines.md#7102-error-condition-responses. </summary>
    public class AzureCoreFoundationsInnerError
    {
        /// <summary> Initializes a new instance of AzureCoreFoundationsInnerError. </summary>
        internal AzureCoreFoundationsInnerError()
        {
        }

        /// <summary> Initializes a new instance of AzureCoreFoundationsInnerError. </summary>
        /// <param name="code"> One of a server-defined set of error codes. </param>
        /// <param name="innererror"> Inner error. </param>
        internal AzureCoreFoundationsInnerError(string code, AzureCoreFoundationsInnerError innererror)
        {
            Code = code;
            Innererror = innererror;
        }

        /// <summary> One of a server-defined set of error codes. </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; }
        /// <summary> Inner error. </summary>
        [JsonProperty(PropertyName = "innererror")]
        public AzureCoreFoundationsInnerError Innererror { get; }
    }
}
