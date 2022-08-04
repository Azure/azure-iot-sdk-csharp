// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Parameters to execute a direct method on the device
    /// </summary>
    public class DirectMethodRequest
    {
        /// <summary>
        /// The method name to run.
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// The timeout (in seconds) before the direct method request will fail if the device doesn't respond to the request.
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? ResponseTimeout { get; set; }

        /// <summary>
        /// The timeout (in seconds) before the direct method request will fail if the request takes too long to reach the device.
        /// This timeout may happen if the target device is not connected to the cloud or if the cloud fails to deliver
        /// the request to the target device in time. If this value is set to 0 seconds, then the target device must be online
        /// when this direct method request is made.
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConnectionTimeout { get; set; }

        [JsonProperty("payload")]
        internal JRaw Payload { get; set; }

        /// <summary>
        /// Set the serialized JSON payload.
        /// </summary>
        public DirectMethodRequest SetPayloadJson(string json)
        {
            if (json == null)
            {
                Payload = null;
            }
            else
            {
                try
                {
                    JToken.Parse(json); // @ailn: this is just a check for valid json as JRaw does not do the validation.
                    Payload = new JRaw(json);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException(ex.Message, nameof(json)); // @ailn: here we want to hide the fact we're using Json.net
                }
            }

            return this; // @ailn: it will allow such code: new CloudToDeviceMethod("c2dmethodname").SetPayloadJson("{ ... }");
        }

        /// <summary>
        /// Get the serialized JSON payload.
        /// </summary>
        public string GetPayloadAsJson()
        {
            // @ailn:
            //  JRaw inherits from JToken which implements explicit string operator.
            //  It takes care of null ref and performs to string logic.
            return (string)Payload;
        }
    }
}
