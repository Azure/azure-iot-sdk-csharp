// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains properties of a job.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/rest/api/iothub/service/createimportexportjob"/>
    public class JobProperties
    {
        private static readonly JobStatus[] s_finishedStates = new[]
        {
            JobStatus.Completed,
            JobStatus.Failed,
            JobStatus.Cancelled,
        };

        /// <summary>
        /// The unique Id of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; set; }

        /// <summary>
        /// When the job started running.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "startTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartedOnUtc { get; internal set; }

        /// <summary>
        /// When the job finished.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "endTimeUtc", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndedOnUtc { get; internal set; }

        /// <summary>
        /// The type of job to execute.
        /// </summary>
        /// <remarks>
        /// This value is set by this client depending on which job method is called.
        /// </remarks>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public JobType Type { get; internal set; }

        /// <summary>
        /// The status of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "status", NullValueHandling = NullValueHandling.Ignore)]
        public JobStatus Status { get; internal set; }

        /// <summary>
        /// If status == failure, this represents a string containing the reason.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "failureReason", NullValueHandling = NullValueHandling.Ignore)]
        public string FailureReason { get; internal set; }

        /// <summary>
        /// URI to a blob container that contains registry data to sync. Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// </summary>
        /// <remarks>
        /// For import job, if there are errors they will be written to OutputBlobContainerUri to a file called "importerrors.log"
        /// </remarks>
        [JsonProperty(PropertyName = "inputBlobContainerUri", NullValueHandling = NullValueHandling.Ignore)]
        public string InputBlobContainerUri { get; set; }

        /// <summary>
        /// URI to a blob container. This is used to output the status of the job and the results. Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// </summary>
        [JsonProperty(PropertyName = "outputBlobContainerUri", NullValueHandling = NullValueHandling.Ignore)]
        public string OutputBlobContainerUri { get; set; }

        /// <summary>
        /// The blob name to be used when importing from the provided input blob container.
        /// </summary>
        /// <remarks>
        /// If not specified, the hub defaults to "devices.txt".
        /// The format should be newline-delimited json objects representing each device twin.
        /// </remarks>
        [JsonProperty(PropertyName = "inputBlobName", NullValueHandling = NullValueHandling.Ignore)]
        public string InputBlobName { get; set; }

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

        /// <summary>
        /// Specifies authentication type being used for connecting to storage account.
        /// </summary>
        [JsonProperty(PropertyName = "storageAuthenticationType", NullValueHandling = NullValueHandling.Ignore)]
        public StorageAuthenticationType? StorageAuthenticationType { get; set; }

        /// <summary>
        /// The managed identity used to access the storage account for import and export jobs.
        /// </summary>
        [JsonProperty(PropertyName = "identity", NullValueHandling = NullValueHandling.Ignore)]
        public ManagedIdentity Identity { get; set; }

        /// <summary>
        /// Whether or not to include configurations in the import or export job.
        /// </summary>
        /// <remarks>
        /// The service assumes this is false, if not specified. If true, then configurations are included in the data export/import.
        /// </remarks>
        [JsonProperty(PropertyName = "includeConfigurations", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncludeConfigurations { get; set; }

        /// <summary>
        /// Specifies the name of the blob to use when exporting/importing configurations.
        /// </summary>
        /// <remarks>
        /// The service assumes this is configurations.txt, if not specified.
        /// </remarks>
        [JsonProperty(PropertyName = "configurationsBlobName", NullValueHandling = NullValueHandling.Ignore)]
        public string ConfigurationsBlobName { get; set; }

        /// <summary>
        /// Convenience property to determine if the job is in a terminal state, based on <see cref="JobStatus"/>.
        /// </summary>
        [JsonIgnore]
        public bool IsFinished => s_finishedStates.Contains(Status);

        /// <summary>
        /// Represents the percentage of completion.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        /// <remarks>The service doesn't actually seem to set this, so not exposing it.</remarks>
        [JsonProperty(PropertyName = "progress", NullValueHandling = NullValueHandling.Ignore)]
        internal int Progress { get; set; }

        /// <summary>
        /// Creates an instance of this class with parameters ready to start an import job.
        /// </summary>
        /// <param name="inputBlobContainerUri">URI to a blob container that contains registry data to sync. Including a SAS token is dependent on the
        /// <see cref="StorageAuthenticationType" /> parameter.</param>
        /// <param name="outputBlobContainerUri">URI to a blob container. This is used to output the status of the job and the results. Including a SAS
        /// token is dependent on the <see cref="StorageAuthenticationType" /> parameter.</param>
        /// <param name="inputBlobName">The blob name to be used when importing from the provided input blob container.</param>
        /// <param name="storageAuthenticationType">Specifies authentication type being used for connecting to storage account.</param>
        /// <param name="identity">User assigned managed identity used to access storage account for import and export jobs.</param>
        /// <returns>An instance of this class.</returns>
        public static JobProperties CreateForImportJob(
            Uri inputBlobContainerUri,
            Uri outputBlobContainerUri,
            string inputBlobName = null,
            StorageAuthenticationType? storageAuthenticationType = null,
            ManagedIdentity identity = null)
        {
            return new JobProperties
            {
                Type = JobType.ImportDevices,
                InputBlobContainerUri = inputBlobContainerUri.ToString(),
                OutputBlobContainerUri = outputBlobContainerUri.ToString(),
                InputBlobName = inputBlobName,
                StorageAuthenticationType = storageAuthenticationType,
                Identity = identity,
            };
        }

        /// <summary>
        /// Creates an instance of this class with parameters ready to start an export job.
        /// </summary>
        /// <param name="outputBlobContainerUri">URI to a blob container. This is used to output the status of the job and the results. Including a SAS token
        /// is dependent on the <see cref="StorageAuthenticationType" /> parameter.</param>
        /// <param name="excludeKeysInExport">Indicates if authorization keys are included in export output</param>
        /// <param name="outputBlobName">The name of the blob that will be created in the provided output blob container</param>
        /// <param name="storageAuthenticationType">Specifies authentication type being used for connecting to storage account</param>
        /// <param name="identity">User assigned managed identity used to access storage account for import and export jobs.</param>
        /// <returns>An instance of this class.</returns>
        public static JobProperties CreateForExportJob(
            Uri outputBlobContainerUri,
            bool excludeKeysInExport,
            string outputBlobName = null,
            StorageAuthenticationType? storageAuthenticationType = null,
            ManagedIdentity identity = null)
        {
            return new JobProperties
            {
                Type = JobType.ExportDevices,
                OutputBlobContainerUri = outputBlobContainerUri.ToString(),
                ExcludeKeysInExport = excludeKeysInExport,
                OutputBlobName = outputBlobName,
                StorageAuthenticationType = storageAuthenticationType,
                Identity = identity,
            };
        }
    }
}
