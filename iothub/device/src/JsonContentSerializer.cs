// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    ///
    /// </summary>
    public class JsonContentSerializer : ISerializer
    {
        /// <summary>
        ///
        /// </summary>
        internal const string ApplicationJson = "application/json";

        /// <summary>
        ///
        /// </summary>
        public static readonly JsonContentSerializer Instance = new JsonContentSerializer();

        /// <summary>
        ///
        /// </summary>
        public string ContentType => ApplicationJson;

        /// <summary>
        ///
        /// </summary>
        /// <param name="objectToSerialize"></param>
        /// <returns></returns>
        public string SerializeToString(object objectToSerialize)
        {
            return JsonConvert.SerializeObject(objectToSerialize);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringToDeserialize"></param>
        /// <returns></returns>
        public T DeserializeToType<T>(string stringToDeserialize)
        {
            return JsonConvert.DeserializeObject<T>(stringToDeserialize);
        }
    }
}
