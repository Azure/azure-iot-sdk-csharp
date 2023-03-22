// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The payload convention class.
    /// </summary>
    public abstract class PayloadConvention
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
        /// The encoding used for the payload.
        /// </summary>
        public abstract Encoding ContentEncoding { get; }

        /// <summary>
        /// Returns the byte array for the convention-based serialized/encoded message.
        /// </summary>
        /// <returns>The correctly encoded object for this convention.</returns>
        public abstract byte[] GetObjectBytes(object objectToSendWithConvention);

        /// <summary>
        /// Returns the object as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="jsonObjectAsText">The object as JSON text to convert.</param>
        /// <returns>The converted object.</returns>
        public abstract T GetObject<T>(string jsonObjectAsText);

        /// <summary>
        /// Returns the object as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="objectToConvert">The object to convert.</param>
        /// <returns>The converted object.</returns>
        public abstract T GetObject<T>(byte[] objectToConvert);

        /// <summary>
        /// Returns the object as the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <param name="streamToConvert">The stream to convert.</param>
        /// <returns>The converted object.</returns>
        public abstract Task<T> GetObjectAsync<T>(Stream streamToConvert);
    }
}
