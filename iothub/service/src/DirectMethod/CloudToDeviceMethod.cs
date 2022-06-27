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
    public class CloudToDeviceMethod
    {
        // @ailn: for serialization only
        internal CloudToDeviceMethod()
        {
        }

        /// <summary>
        /// Creates an instance of CloudToDeviceMethod type
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="responseTimeout">Method timeout</param>
        /// <param name="connectionTimeout">Device connection timeout</param>
        /// <exception cref="ArgumentException">If <b>methodName</b> is null or whitespace</exception>
        public CloudToDeviceMethod(string methodName, TimeSpan responseTimeout, TimeSpan connectionTimeout)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Canot be empty", nameof(methodName));
            }

            MethodName = methodName;
            ResponseTimeout = responseTimeout;
            ConnectionTimeout = connectionTimeout;
        }

        /// <summary>
        /// Creates an instance of CloudToDeviceMethod type with Zero timeout
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="responseTimeout">Method timeout</param>
        /// <exception cref="ArgumentException">If <b>methodName</b> is null or whitespace</exception>
        public CloudToDeviceMethod(string methodName, TimeSpan responseTimeout)
            : this(methodName, responseTimeout, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Creates an instance of CloudToDeviceMethod type with Zero timeout
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <exception cref="ArgumentException">If <b>methodName</b> is null or whitespace</exception>
        public CloudToDeviceMethod(string methodName)
            : this(methodName, TimeSpan.Zero, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Method to run
        /// </summary>
        [JsonProperty("methodName", Required = Required.Always)]
        public string MethodName { get; set; }

        /// <summary>
        /// Method timeout
        /// </summary>
        [JsonIgnore]
        public TimeSpan ResponseTimeout { get; set; }

        /// <summary>
        /// Timeout for device to come online
        /// </summary>
        [JsonIgnore]
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Method timeout in seconds
        /// </summary>
        [JsonProperty("responseTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ResponseTimeoutInSeconds => ResponseTimeout <= TimeSpan.Zero
            ? (int?)null
            : (int)ResponseTimeout.TotalSeconds;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        [JsonProperty("connectTimeoutInSeconds", NullValueHandling = NullValueHandling.Ignore)]
        internal int? ConnectionTimeoutInSeconds => ConnectionTimeout <= TimeSpan.Zero
            ? (int?)null
            : (int)ConnectionTimeout.TotalSeconds;

        [JsonProperty("payload")]
        internal JRaw Payload { get; set; }

        /// <summary>
        /// Set payload as json
        /// </summary>
        public CloudToDeviceMethod SetPayloadJson(string json)
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
        /// Get payload as json
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
