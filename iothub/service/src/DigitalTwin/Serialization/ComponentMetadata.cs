// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// An optional, helper class for deserializing a digital twin.
    /// The $metadata class on a component of a <see cref="BasicDigitalTwin"/>.
    /// </summary>
    public class ComponentMetadata
    {
        /// <summary>
        /// Model-defined writable properties' request state.
        /// </summary>
        /// <remarks>For convenience, the value of each dictionary object can be turned into an instance of <see cref="WritableProperty"/>.</remarks>
        [JsonExtensionData]
        public IDictionary<string, object> WritableProperties { get; } = new Dictionary<string, object>();
    }
}
