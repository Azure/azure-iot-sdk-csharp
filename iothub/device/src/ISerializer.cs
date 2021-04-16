// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Provides the serialzation for a specified convention
    /// </summary>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="stringToDeserialize"></param>
        /// <returns></returns>
        public abstract T DeserializeToType<T>(string stringToDeserialize);
    }
}
