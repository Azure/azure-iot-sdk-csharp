// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Parameters to execute a direct method on a device or module.
    /// </summary>
    public class DirectMethodServiceRequest
    {
        /// <summary>
        /// Initialize an instance of this class.
        /// </summary>
        /// <param name="methodName">The method name to run.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="methodName"/> is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="methodName"/> is empty or white space.</exception>
        [JsonConstructor]
        public DirectMethodServiceRequest(string methodName)
        {
            Argument.AssertNotNullOrWhiteSpace(methodName, nameof(methodName));
            MethodName = methodName;
            Payload = null;
        }

        /// <summary>
        /// The method name to run.
        /// </summary>
        [JsonPropertyName("methodName")]
        public string MethodName { get; }

        /// <summary>
        /// The serialized and encoded payload bytes.
        /// </summary>
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; private set; }

        /// <summary>
        /// The amount of time (in seconds) given to the service to connect to the device.
        /// </summary>
        /// <remarks>
        /// A timeout may occur if this value is set to zero and the target device is not connected to
        /// the cloud.
        /// If the value is greater than zero, it may also occur if the cloud fails to deliver the request to
        /// the target device.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, a value of <c>TimeSpan.FromMilliseconds(500)</c> will be
        /// interpreted as 0 seconds (using <c>ConnectTimeout.TotalSeconds</c>).
        /// </para>
        /// </remarks>
        [JsonPropertyName("connectTimeoutInSeconds")]
        public int? ConnectTimeoutInSeconds { get; set; }

        /// <summary>
        /// The amount of time (in seconds) given to the device to process and respond to the command request.
        /// </summary>
        /// <remarks>
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </para>
        /// </remarks>
        [JsonPropertyName("responseTimeoutInSeconds")]
        public int? ResponseTimeoutInSeconds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetPayload(int value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetPayload(string value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetPayload(bool value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonString"></param>
        public void SetPayloadJson(string jsonString)
        {
            Payload = JsonDocument.Parse(jsonString).RootElement;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetPayloadJson(JsonElement value)
        {
            Payload = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SetPayload(object value)
        {
            SetPayloadJson(JsonSerializer.Serialize(value));
        }
    }
}
