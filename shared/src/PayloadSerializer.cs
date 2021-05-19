// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// Provides the serialization for a specified convention.
    /// </summary>
    /// <remarks>
    /// The serializer is responsible for converting all of your objects into the correct format for the <see cref="PayloadConvention"/> that uses it.
    /// <para>
    /// By default we have implemented the <see cref="NewtonsoftJsonPayloadSerializer"/> class that uses <see cref="Newtonsoft.Json.JsonConvert"/>
    /// to handle the serialization for the <see cref="DefaultPayloadConvention"/> class.
    /// </para>
    /// </remarks>
    public abstract class PayloadSerializer
    {
        /// <summary>
        /// Used to specify what type of content to expect.
        /// </summary>
        /// <remarks>This can be free-form but should adhere to standard MIME types. For example, "application/json" is what we implement by default.</remarks>
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
        /// <typeparam name="T">The type you want to return.</typeparam>
        /// <param name="stringToDeserialize">String to deserialize.</param>
        /// <returns>A fully deserialized type.</returns>
        public abstract T DeserializeToType<T>(string stringToDeserialize);

        /// <summary>
        /// Converts the object using the serializer.
        /// </summary>
        /// <remarks>This class is used by the PayloadCollection-based classes to attempt to convert from the native serializer type
        /// (for example, JObject or JsonElement) to the desired type.
        /// When you implement this you need to be aware of what type your serializer will use for anonymous types.</remarks>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>A converted object</returns>
        public abstract T ConvertFromObject<T>(object objectToConvert);

        /// <summary>
        /// Gets a nested property from the serialized data.
        /// </summary>
        /// <remarks>
        /// This is used internally by our PayloadCollection-based classes to attempt to get a property of the underlying object.
        /// An example of this would be a property under the component.
        /// </remarks>
        /// <typeparam name="T">The type to convert the retrieved property to.</typeparam>
        /// <param name="nestedObject">The object that might contain the nested property.</param>
        /// <param name="propertyName">The name of the property to be retrieved.</param>
        /// <returns>True if the nested object contains an element with the specified key; otherwise, it returns false.</returns>
        /// <param name="outValue"></param>
        public abstract bool TryGetNestedObjectValue<T>(object nestedObject, string propertyName, out T outValue);

        /// <summary>
        /// Creates the correct <see cref="IWritablePropertyResponse"/> to be used with this serializer.
        /// </summary>
        /// <param name="value">The value of the property.</param>
        /// <param name="statusCode">The status code of the write operation.</param>
        /// <param name="version">The version the property is responding to.</param>
        /// <param name="description">An optional description of the writable property response.</param>
        /// <returns>The writable property response to be used with this serializer.</returns>
        public abstract IWritablePropertyResponse CreateWritablePropertyResponse(object value, int statusCode, long version, string description = default);
    }
}
