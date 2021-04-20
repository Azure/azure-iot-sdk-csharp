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
        public abstract string ContentType { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public abstract string SerializeToString(object objectToSerialize);

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public abstract T CastFromObject<T>(object objectToSerialize);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringToDeserialize"></param>
        /// <returns></returns>
        public abstract T DeserializeToType<T>(string stringToDeserialize);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeToCheck"></param>
        /// <returns></returns>object
        public abstract bool CheckType(object typeToCheck);
    }
}
