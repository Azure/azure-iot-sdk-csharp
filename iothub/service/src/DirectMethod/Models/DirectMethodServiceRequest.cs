// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public string MethodName { get; set; }

        /// <summary>
        /// The serialized and encoded payload bytes.
        /// </summary>
        [JsonPropertyName("payload")]
        public JsonElement? Payload { get; set; }

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
        /// Set the payload as a single integer Json value.
        /// </summary>
        /// <param name="value">The integer Json value.</param>
        public void SetPayload(int value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// Set the payload as a single string Json value.
        /// </summary>
        /// <param name="value">The string Json value.</param>
        /// <remarks>This method should not be confused with <see cref="SetPayloadJson(string)"/> which allows you to pass in a Json object (rather than a Json value).</remarks>
        public void SetPayload(string value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// Set the payload as a single boolean Json value.
        /// </summary>
        /// <param name="value">The boolean Json value.</param>
        public void SetPayload(bool value)
        {
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// Set the payload as a single DateTimeOffset Json value.
        /// </summary>
        /// <param name="value">The DateTimeOffset Json value.</param>
        public void SetPayload(DateTimeOffset value)
        { 
            Payload = JsonSerializer.SerializeToElement(value);
        }

        /// <summary>
        /// Set the payload as a Json object string.
        /// </summary>
        /// <param name="jsonString">A valid Json object string such as "{\"someKey\":\"someValue\"}"</param>
        /// <remarks>This method should not be confused with <see cref="SetPayload(string)"/> which allows users to set the payload as a single Json value (like "someJsonValue").</remarks>
        public void SetPayloadJson(string jsonString)
        {
            Payload = JsonDocument.Parse(jsonString).RootElement;
        }

        /// <summary>
        /// Set the payload as a single unmodeled Json object.
        /// </summary>
        /// <param name="value">The unmodeled Json object.</param>
        public void SetPayloadJson(JsonElement value)
        {
            Payload = value;
        }

        /// <summary>
        /// Set the payload as a modeled object that System.Text.Json can serialize for you.
        /// </summary>
        /// <param name="value">the model type</param>
        /// <remarks>This allows you to pass in strongly typed objects (where fields are marked with <see cref="JsonPropertyNameAttribute"/>).</remarks>
        public void SetPayload(object value)
        {
            SetPayloadJson(JsonSerializer.Serialize(value));
        }
    }
}
