// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Provides the serialzation for a specified convention
    /// </summary>
    /// <remarks>
    /// The serializer is responsible for converting all of your objects into the correct format for the <see cref="IPayloadConvention"/> that uses it.
    /// <para>
    /// By default we have implemented the <see cref="JsonContentSerializer"/> class that uses <see cref="Newtonsoft.Json.JsonConvert"/> to handle the serialization for the <see cref="DefaultPayloadConvention"/> class. 
    /// </para>
    /// </remarks>
    public abstract class ISerializer
    {
        /// <summary>
        /// Used to specify what type of content to expect
        /// </summary>
        /// <value>A string representing the content type to use when sending a payload</value>
        /// <remarks>This can be freeform but should adhere to standard MIME types. For example, "application/json" is what we implement by default.</remarks>
        public abstract string ContentType { get; }

        /// <summary>
        /// Serialize the specified object to a string
        /// </summary>
        /// <param name="objectToSerialize">Object to serialize.</param>
        /// <returns>A serilaized string of the object.</returns>
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
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>A converted object</returns>
        /// <remarks>This class is used by the <see cref="PropertyCollection"/> to attempt to convert from the native serailizer type (for example, JObject or JsonElement) to the desired type. When you implement this you need to be aware of what type your serializer will use for anonymous types.</remarks>
        public abstract T ConvertFromObject<T>(object objectToConvert);

        /// <summary>
        /// Checks to make sure the type of <see cref="WritablePropertyBase"/> can be properly serialized by this class.
        /// </summary>
        /// <param name="typeToCheck"></param>
        /// <returns><c>true</c> if the type is supported; <c>false</c> if it is not</returns>
        public abstract bool CheckWritablePropertyType(object typeToCheck);
    }
}
