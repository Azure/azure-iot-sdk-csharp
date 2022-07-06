// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// A class for creating and serializing a <see cref="Configuration"/> for a bulk import
    /// job using <see cref="DevicesClient.CreateJobAsync(JobProperties, System.Threading.CancellationToken)"/>.
    /// </summary>
    public class ImportConfiguration : Configuration
    {
        /// <inheritdoc/>
        public ImportConfiguration(string configurationId)
            : base(configurationId)
        {
        }

        /// <summary>
        /// The type of registry operation and E Tag preferences.
        /// </summary>
        [JsonProperty(PropertyName = "importMode", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public ConfigurationImportMode ImportMode { get; set; }
    }
}
