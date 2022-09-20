// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of an export job.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/rest/api/iothub/service/createimportexportjob"/>
    public class ExportJobProperties : JobProperties
    {
        /// <summary>
        /// Serialization constructor.
        /// </summary>
        internal ExportJobProperties()
        { }

        /// <summary>
        /// Creates an instance of this class for the export job.
        /// </summary>
        /// <seealso cref="DevicesClient.ExportAsync(ExportJobProperties, System.Threading.CancellationToken)"/>
        /// <param name="outputBlobContainerUri">URI to a blob container, used to output the status of the job and the results.</param>
        /// <param name="excludeKeysInExport">Whether to include authorization keys in export output.</param>
        public ExportJobProperties(Uri outputBlobContainerUri, bool excludeKeysInExport)
        {
            Type = JobType.ExportDevices;
            OutputBlobContainerUri = outputBlobContainerUri;
            ExcludeKeysInExport = excludeKeysInExport;
        }

        /// <summary>
        /// The name of the blob that will be created in the provided output blob container. This blob will contain
        /// the exported device registry information for the IoT hub.
        /// </summary>
        /// <remarks>
        /// If not specified, defaults to "devices.txt".
        /// </remarks>
        [JsonProperty(PropertyName = "outputBlobName", NullValueHandling = NullValueHandling.Ignore)]
        public string OutputBlobName { get; set; }

        /// <summary>
        /// Optional for export jobs; ignored for other jobs. Default: false. If false, authorization keys are included
        /// in export output. Keys are exported as null otherwise.
        /// </summary>
        [JsonProperty(PropertyName = "excludeKeysInExport", NullValueHandling = NullValueHandling.Ignore)]
        public bool ExcludeKeysInExport { get; set; }
    }
}
