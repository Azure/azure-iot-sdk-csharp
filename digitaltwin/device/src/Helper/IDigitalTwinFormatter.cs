// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Helper
{
    /// <summary>
    /// The Interface of Digital Twin Serializer.
    /// </summary>
    internal interface IDigitalTwinFormatter
    {
        /// <summary>
        /// Serialize to string.
        /// </summary>
        /// <typeparam name="T">Any class or struct.</typeparam>
        /// <param name="userObject">The object needs to be serialized.</param>
        /// <returns>The serialized string.</returns>
        string FromObject<T>(T userObject);

        /// <summary>
        /// Serialize to string.
        /// </summary>
        /// <typeparam name="T">Any class or struct.</typeparam>
        /// <param name="value">The string.</param>
        /// <returns>The instance needs to be de-serialized.</returns>
        T ToObject<T>(string value);
    }
}
