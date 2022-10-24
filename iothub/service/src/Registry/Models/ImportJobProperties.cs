// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of an import job.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/rest/api/iothub/service/createimportexportjob"/>
    public class ImportJobProperties : JobProperties
    {
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        internal ImportJobProperties()
        { }

        /// <summary>
        /// Creates an instance of this class for the import job.
        /// </summary>
        /// <seealso cref="DevicesClient.ImportAsync(ImportJobProperties, System.Threading.CancellationToken)"/>
        /// <param name="inputBlobContainerUri">URI to a blob container that contains registry data to sync.</param>
        /// <param name="outputBlobContainerUri">URI to a blob container, used to output the status of the job and the results.
        /// If not specified, the input blob container will be used.</param>
        public ImportJobProperties(Uri inputBlobContainerUri, Uri outputBlobContainerUri = default)
        {
            JobType = JobType.ImportDevices;
            InputBlobContainerUri = inputBlobContainerUri;
            OutputBlobContainerUri = outputBlobContainerUri ?? inputBlobContainerUri;
        }

        /// <summary>
        /// URI to a blob container that contains registry data to sync.
        /// </summary>
        /// <remarks>
        /// Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// </remarks>
        [JsonProperty(PropertyName = "inputBlobContainerUri", NullValueHandling = NullValueHandling.Ignore)]
        public Uri InputBlobContainerUri { get; set; }

        /// <summary>
        /// The blob name to be used when importing from the provided input blob container.
        /// </summary>
        /// <remarks>
        /// If not specified, the hub defaults to "devices.txt".
        /// The format should be newline-delimited json objects representing each device twin.
        /// </remarks>
        [JsonProperty(PropertyName = "inputBlobName", NullValueHandling = NullValueHandling.Ignore)]
        public string InputBlobName { get; set; }
    }
}
