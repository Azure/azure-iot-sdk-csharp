// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Provides the serialization for a specified convention.
    /// </summary>
    /// <remarks>
    /// The serializer is responsible for converting all objects into the correct format for the <see cref="PayloadConvention"/> that uses it.
    /// <para>
    /// By default there are implementions the <see cref="NewtonsoftJsonPayloadSerializer"/> class that uses <see cref="Newtonsoft.Json.JsonConvert"/>
    /// to handle the serialization for the <see cref="DefaultPayloadConvention"/> class.
    /// </para>
    /// </remarks>
    public abstract class PayloadSerializer
    {
        /// <summary>
        /// Used to specify what type of content will be in the payload.
        /// </summary>
        /// <remarks>
        /// This can be free-form but should adhere to standard <see href="https://docs.w3cub.com/http/basics_of_http/mime_types.html">MIME types</see>,
        /// for example: "application/json".
        /// </remarks>
        /// <value>A string representing the content type to use when sending a payload.</value>
        public abstract string ContentType { get; }

        /// <summary>
        /// Serialize the specified object to a string.
        /// </summary>
        /// <param name="objectToSerialize">Object to serialize.</param>
        /// <returns>A serialized string of the object.</returns>
        public abstract string SerializeToString(object objectToSerialize);

        /// <summary>
        /// Convert the serialized string to an object.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="stringToDeserialize">The string to deserialize.</param>
        /// <returns>A fully deserialized type.</returns>
        public abstract T DeserializeToType<T>(string stringToDeserialize);

        /// <summary>
        /// Converts the JSON object using the serializer.
        /// </summary>
        /// <remarks>
        /// This class is used to attempt to convert from the native serializer type
        /// (for example, JObject or JsonElement) to the desired type.
        /// When implementing this, be aware of what type the serializer will use for anonymous types.
        /// </remarks>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="jsonObjectToConvert">The object to convert.</param>
        /// <returns>A converted object</returns>
        public abstract T ConvertFromJsonObject<T>(object jsonObjectToConvert);
    }
}
