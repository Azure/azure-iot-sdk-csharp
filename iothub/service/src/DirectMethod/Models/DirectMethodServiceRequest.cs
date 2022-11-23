// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        /// Creates an instance of this class with the specified payload to be serialized.
        /// </summary>
        /// <param name="payload">The method serializable payload to send to the device.</param>
        public DirectMethodServiceRequest(object payload = default)
        {
            if (payload != default)
            {
                using var jd = JsonDocument.Parse(JsonSerializer.Serialize(payload));
                JsonPayload = jd.RootElement;
            }
        }

        /// <summary>
        /// The method name to run.
        /// </summary>
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        /// <summary>
        /// The amount of time given to the service to connect to the device.
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
        [JsonIgnore]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// The amount of time given to the device to process and respond to the command request.
        /// </summary>
        /// <remarks>
        /// This timeout may happen if the target device is slow in handling the direct method.
        /// <para>
        /// This value is propagated to the service in terms of seconds, so this value does not have a level of
        /// precision below seconds. For example, setting this value to TimeSpan.FromMilliseconds(500) will result
        /// in this request having a timeout of 0 seconds.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// Get the serialized JSON payload.
        /// </summary>
        /// <remarks>
        /// May be null or empty.
        /// </remarks>
        [JsonIgnore]
        public string PayloadAsJsonString => JsonPayload?.GetRawText();

        /// <summary>
        /// The deserialized payload.
        /// </summary>
        [JsonPropertyName("payload")]
        internal JsonElement? JsonPayload { get; set; }

        [JsonPropertyName("responseTimeoutInSeconds")]
        internal int? ResponseTimeoutInSeconds => (int?)ResponseTimeout?.TotalSeconds ?? null;

        [JsonPropertyName("connectTimeoutInSeconds")]
        internal int? ConnectionTimeoutInSeconds => (int?)ConnectionTimeout?.TotalSeconds ?? null;

        /// <summary>
        /// Returns JSON payload as a specified type.
        /// </summary>
        /// <typeparam name="T">The type into which the JSON payload can be deserialized.</typeparam>
        /// <returns>True if the value could be deserialized, otherwise false.</returns>
        public bool TryGetPayload<T>(out T value)
        {
            value = default;

            try
            {
                value = JsonSerializer.Deserialize<T>(PayloadAsJsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
