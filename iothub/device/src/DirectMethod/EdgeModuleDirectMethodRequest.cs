// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Parameters to execute a direct method on an edge device or an edge module by an <see cref="IotHubModuleClient"/>.
    /// </summary>
    public class EdgeModuleDirectMethodRequest
    {
        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        public EdgeModuleDirectMethodRequest(string methodName)
            : this(methodName, null)
        {
        }

        /// <summary>
        /// A direct method request to be initialized by the client application when using an <see cref="IotHubModuleClient"/> for invoking
        /// a direct method on an edge device or an edge module connected to the same edge hub.
        /// </summary>
        /// <param name="methodName">The method name to invoke.</param>
        /// <param name="payload">The direct method payload that will be serialized.</param>
        [JsonConstructor]
        public EdgeModuleDirectMethodRequest(string methodName, byte[] payload)
        {
            MethodName = methodName;
            Payload = payload;
        }

        /// <summary>
        /// The method name to invoke.
        /// </summary>
        [JsonPropertyName("methodName")]
        public string MethodName { get; }

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
        [JsonPropertyName("connectTimeoutInSeconds")]
        public int? ConnectTimeoutInSeconds { get; set; }

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
        [JsonPropertyName("responseTimeoutInSeconds")]
        public int? ResponseTimeoutInSeconds { get; set; }

        /// <summary>
        /// The direct method payload.
        /// </summary>
        [JsonPropertyName("payload")]
        public byte[] Payload { get; set; }

        /// <summary>
        /// The direct method request payload, deserialized to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the direct method request payload to.</typeparam>
        /// <param name="payload">When this method returns true, this contains the value of the direct method request payload.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if the direct method request payload can be deserialized to type <c>T</c>; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code language="csharp">
        /// await client.SetDirectMethodCallbackAsync((edgeModuleDirectMethodRequest) =>
        /// {
        ///     if (edgeModuleDirectMethodRequest.TryGetPayload(out MyCustomType customTypePayload))
        ///     {
        ///         // do work
        ///         // ...
        ///         
        ///         // Acknowlege the direct method call with the status code 200.  
        ///         return Task.FromResult(new DirectMethodResponse(200));
        ///     }
        ///     else
        ///     {
        ///         // Acknowlege the direct method call the status code 400.
        ///         return Task.FromResult(new DirectMethodResponse(400));
        ///     }
        ///     
        ///     
        ///     // ...
        /// },
        /// cancellationToken);
        /// </code>
        /// </example>
        public bool TryGetPayload<T>(out T payload)
        {
            payload = default;

            try
            {
                payload = JsonSerializer.Deserialize<T>(Payload, JsonSerializerSettings.Options);
                return true;
            }
            catch (Exception ex) when (Logging.IsEnabled)
            {
                Logging.Error(this, $"Unable to convert payload to {typeof(T)} due to {ex}", nameof(TryGetPayload));
            }

            return false;
        }
    }
}