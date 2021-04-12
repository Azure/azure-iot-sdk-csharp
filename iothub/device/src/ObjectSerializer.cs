// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Default serialization and deserialization settings
    /// </summary>
    public class ObjectSerializer
    {
        /// <summary>
        ///
        /// </summary>
        internal const string ApplicationJson = "application/json";

        /// <summary>
        ///
        /// </summary>
        public static readonly ObjectSerializer Instance = new ObjectSerializer();

        /// <summary>
        ///
        /// </summary>
        public string ContentType { get; set; } = ApplicationJson;

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public virtual string SerializeToString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringToDeserialize"></param>
        /// <returns></returns>
        public virtual T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonConvert.DeserializeObject<T>(stringToDeserialize);
        }
    }
}
