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
        /// Serialization constructor
        /// </summary>
        internal JobProperties() { }

        /// <summary>
        /// Creates an instance of this class for the import job.
        /// </summary>
        /// <seealso cref="DevicesClient.ImportAsync(JobProperties, System.Threading.CancellationToken)"/>
        /// <remarks>
        /// Other relevant, optional properties to set include:
        /// <list type="bullet">
        /// <item>InputBlobName, to override the default.</item>
        /// <item>StorageAuthenticationType, if using identity-based instead of SAS in InputBlobContainerUri.</item>
        /// <item>ManagedIdentity, if the IoT hub is not configured with a managed identity and the service app will specify
        /// which identity has permissions to the storage account.</item>
        /// <item>IncludeConfigurations, if the import job should also include configurations.</item>
        /// <item>ConfigurationsBlobName, if the import job includes configurations and to override the default blob name.</item>
        /// </list>
        /// </remarks>
        /// <param name="inputBlobContainerUri">URI to a blob container that contains registry data to sync.</param>
        /// <param name="outputBlobContainerUri">URI to a blob container, used to output the status of the job and the results.
        /// If not specified, the input blob container will be used.</param>
        public JobProperties(Uri inputBlobContainerUri, Uri outputBlobContainerUri = default)
        {
            Type = JobType.ImportDevices;
            InputBlobContainerUri = inputBlobContainerUri;
            OutputBlobContainerUri = outputBlobContainerUri ?? inputBlobContainerUri;
        }

        /// <summary>
        /// Creates an instance of this class for the export job.
        /// </summary>
        /// <seealso cref="DevicesClient.ExportAsync(JobProperties, System.Threading.CancellationToken)"/>
        /// <remarks>
        /// Other relevant, optional properties to set include:
        /// <list type="bullet">
        /// <item>OutputBlobName, to override the default.</item>
        /// <item>StorageAuthenticationType, if using identity-based instead of SAS in InputBlobContainerUri.</item>
        /// <item>ManagedIdentity, if the IoT hub is not configured with a managed identity and the service app will specify
        /// which identity has permissions to the storage account.</item>
        /// <item>IncludeConfigurations, if the export job should also include configurations.</item>
        /// <item>ConfigurationsBlobName, if the export job includes configurations and to override the default blob name.</item>
        /// </list>
        /// </remarks>
        /// <param name="outputBlobContainerUri">URI to a blob container, used to output the status of the job and the results.</param>
        /// <param name="excludeKeysInExport">Whether to include authorization keys in export output.</param>
        public JobProperties(Uri outputBlobContainerUri, bool excludeKeysInExport)
        {
            Type = JobType.ExportDevices;
            OutputBlobContainerUri = outputBlobContainerUri;
            ExcludeKeysInExport = excludeKeysInExport;
        }

        /// <summary>
        /// The unique Id of the job.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        [JsonProperty(PropertyName = "jobId", NullValueHandling = NullValueHandling.Ignore)]
        public string JobId { get; internal set; }

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
        /// URI to a blob container that contains registry data to sync.
        /// </summary>
        /// <remarks>
        /// Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// </remarks>
        [JsonProperty(PropertyName = "inputBlobContainerUri", NullValueHandling = NullValueHandling.Ignore)]
        public Uri InputBlobContainerUri { get; set; }

        /// <summary>
        /// URI to a blob container, used to output the status of the job and the results.
        /// </summary>
        /// <remarks>
        /// Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// <para>
        /// For import job, if there are errors they will be written to OutputBlobContainerUri to a file called "importerrors.log"
        /// </para>
        /// </remarks>
        [JsonProperty(PropertyName = "outputBlobContainerUri", NullValueHandling = NullValueHandling.Ignore)]
        public Uri OutputBlobContainerUri { get; set; }

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
    }
}
