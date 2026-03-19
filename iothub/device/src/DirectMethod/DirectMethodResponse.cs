// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The device/module's response to a direct method invocation.
    /// </summary>
    public class DirectMethodResponse
    {
        /// <summary>
        /// Initializes an instance of this class.
        /// </summary>
        public DirectMethodResponse(int status)
        {
            Status = status;
        }

        /// <summary>
        /// The status of direct method response.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// The optional direct method payload.
        /// </summary>
        /// <remarks>
        ///  The value of this payload must be the UTF-8 encodeded bytes of a valid Json value (string, int, bool, etc),
        ///  a valid Json array, or a valid Json object. Use functions like <see cref="SetPayload(bool)"/>
        ///  to set this payload equal to primitive types. Use functions like <see cref="SetPayload(JsonElement)"/>
        ///  or <see cref="SetPayloadJson(string)"/> to set this payload as an unmodeled complex json object. Use 
        ///  <see cref="SetPayload(object)"/> to set this payload as a strongly typed object (that is serializable by System.Text.Json)
        /// </remarks>
        public byte[] Payload { get; set; }

        /// <summary>
        /// The request Id for the transport layer.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Set this payload as an integer (a simple JSON value).
        /// </summary>
        /// <param name="value">The JSON value integer</param>
        public void SetPayload(int value)
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(value);
        }

        /// <summary>
        /// Set this payload as a boolean (a JSON value).
        /// </summary>
        /// <param name="value">The JSON value boolean</param>
        public void SetPayload(bool value)
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(value);
        }

        /// <summary>
        /// Set this payload as a string (a simple JSON value).
        /// </summary>
        /// <param name="value">The JSON value string. For instance, "someValue".</param>
        public void SetPayload(string value)
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(value);
        }

        /// <summary>
        /// Set this payload as an arbitrary JSON document.
        /// </summary>
        /// <param name="jsonString">The JSON value string. For instance "{\"someKey\":\"someValue\"}"</param>
        /// <remarks>This function just UTF-8 encodes the provided string. It does not further validation.</remarks>
        public void SetPayloadJson(string jsonString)
        {
            Payload = Encoding.UTF8.GetBytes(jsonString);
        }

        /// <summary>
        /// Set the payload equal to a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="jsonElement">The JsonElement value to assign.</param>
        public void SetPayload(JsonElement jsonElement)
        {
            Payload = Encoding.UTF8.GetBytes(jsonElement.GetRawText());
        }

        /// <summary>
        /// Use a serializable object as the payload.
        /// </summary>
        /// <param name="serializableObject">Any custom payload object that is serializable by System.Text.Json</param>
        /// <remarks>
        /// This object must be serializable by System.Text.Json
        /// </remarks>
        public void SetPayload(object serializableObject)
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(serializableObject);
        }

        /// <summary>
        /// The direct method response payload, deserialized to the specified type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One usage of this method is to deserialize the direct method response received by an edge module client
        /// after it invokes a direct method on an edge device or an edge module connected to the same edge hub.
        /// These operations are invoked using the API <see cref="IotHubModuleClient.InvokeMethodAsync(string, EdgeModuleDirectMethodRequest, System.Threading.CancellationToken)"/>
        /// and <see cref="IotHubModuleClient.InvokeMethodAsync(string, string, EdgeModuleDirectMethodRequest, System.Threading.CancellationToken)"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The type to deserialize the direct method response payload to.</typeparam>
        /// <param name="payload">When this method returns true, this contains the value of the direct method response payload.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if the direct method response payload can be deserialized to type <c>T</c>; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <code language="csharp">
        /// DirectMethodResponse response = await client
        ///     .InvokeMethodAsync(deviceId, moduleId, directMethodRequest, cancellationToken)
        ///     .ConfigureAwait(false);
        /// if (response.TryGetPayload(out MyCustomType customTypePayload))
        /// {
        ///     // do work
        ///     // ...
        /// }
        ///
        /// // ...
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
            catch (Exception ex)
            {
                // In case the value cannot be converted using the serializer,
                // then return false with the default value of the type <T> passed in.
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Unable to convert payload to {typeof(T)} due to {ex}", nameof(TryGetPayload));
            }

            return false;
        }
    }
}
