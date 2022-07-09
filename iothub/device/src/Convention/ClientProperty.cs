// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The property value reported by the client.
    /// </summary>
    public class ClientProperty
    {
        // TODO: Unit-testable and mockable

        internal ClientProperty()
        {
        }

        /// <summary>
        /// The name of the component for which the property was reported.
        /// This is <c>null</c> for a root-level property.
        /// </summary>
        public string ComponentName { get; internal set; }

        /// <summary>
        /// The name of the property reported.
        /// </summary>
        public string PropertyName { get; internal set; }

        /// <summary>
        /// The value of the property reported.
        /// </summary>
        public object Value { get; internal set; }

        internal PayloadConvention Convention { get; set; }

        /// <summary>
        /// The value of the property reported, deserialized to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the <paramref name="propertyValue"/> to.</typeparam>
        /// <param name="propertyValue">When this method returns true, this contains the value of the property.
        /// When this method returns false, this contains the default value of the type <c>T</c> passed in.</param>
        /// <returns><c>true</c> if a property of type <c>T</c> was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(out T propertyValue)
        {
            return ObjectConversionHelpers.TryCastOrConvert(Value, Convention, out propertyValue);
        }
    }
}
