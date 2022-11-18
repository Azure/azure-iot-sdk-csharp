// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains the properties available for import/export job.
    /// </summary>
    public abstract class JobProperties : IotHubJobResponse
    {
        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        /// <remarks>
        /// This class can be inherited from and set by unit tests for mocking purposes.
        /// </remarks>
        protected internal JobProperties()
        { }

        /// <summary>
        /// The type of job to execute.
        /// </summary>
        /// <remarks>
        /// This value is set by this client depending on which job method is called.
        /// </remarks>
        [JsonPropertyName("type", Required = Required.Always)]
        public JobType JobType { get; protected internal set; }

        /// <summary>
        /// URI to a blob container, used to output the status of the job and the results.
        /// </summary>
        /// <remarks>
        /// Including a SAS token is dependent on the <see cref="StorageAuthenticationType" /> property.
        /// </remarks>
        [JsonPropertyName("outputBlobContainerUri")]
        public Uri OutputBlobContainerUri { get; set; }

        /// <summary>
        /// Specifies authentication type being used for connecting to storage account.
        /// </summary>
        [JsonPropertyName("storageAuthenticationType")]
        public StorageAuthenticationType? StorageAuthenticationType { get; set; }

        /// <summary>
        /// The managed identity used to access the storage account for the job.
        /// </summary>
        [JsonPropertyName("identity")]
        public ManagedIdentity Identity { get; set; }

        /// <summary>
        /// Whether or not to include configurations in the job.
        /// </summary>
        /// <remarks>
        /// The service assumes this is false, if not specified. If true, then configurations are included in the data import/export.
        /// </remarks>
        [JsonPropertyName("includeConfigurations")]
        public bool? IncludeConfigurations { get; set; }

        /// <summary>
        /// Specifies the name of the blob to use when using configurations.
        /// </summary>
        /// <remarks>
        /// The service assumes this is configurations.txt, if not specified.
        /// </remarks>
        [JsonPropertyName("configurationsBlobName")]
        public string ConfigurationsBlobName { get; set; }

        /// <summary>
        /// Represents the percentage of completion.
        /// </summary>
        /// <remarks>
        /// This value is created by the service. If specified by the user, it will be ignored.
        /// </remarks>
        /// <remarks>The service doesn't actually seem to set this, so not exposing it.</remarks>
        [JsonPropertyName("progress")]
        internal int Progress { get; set; }
    }
}
