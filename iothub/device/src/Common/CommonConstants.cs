// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    internal static class CommonConstants
    {
        public const string DeviceAudienceFormat = "{0}/devices/{1}";
        public const string MediaTypeForDeviceManagementApis = "application/json";
        public const string AmqpsScheme = "amqps";
        public const string AmqpScheme = "amqp";
        public const string AmqpDnsWSPrefix = "amqpws";

        // IotHub WindowsFabric Constants
        public const int WindowsFabricRetryLimit = 20;

        public const int WindowsFabricRetryWaitInMilliseconds = 3000;
        public const string IotHubApplicationName = "fabric:/microsoft.azure.devices.container";
        public const string IotHubApplicationTypeName = "microsoft.azure.devices.container";
        public const string IotHubServiceTypeName = "microsoft.azure.devices.container.service";
        public const string IotHubMetadataParentName = "iothub-metadata";

        public const string MicrosoftOwinContextPropertyName = "MS_OwinContext";

        // EventHub
        public const int EventHubEndpointPortNumber = 5671;

        public const string EventHubConnectionStringTemplate = "{0};PartitionCount={1}";

        // Namespace paths
        public const string ResourceProviderNamespace = "Microsoft.Devices";

        public const string ResourceProviderServiceResourceType = ResourceProviderNamespace + "/IotHubs";
        public const string ResourceProviderBasePathTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/" + ResourceProviderServiceResourceType + "/{2}";

        // Runtime Retry Constants
        public const int RuntimeRetryLimit = 3;

        public const int RuntimeRetryWaitInMilliseconds = 5000;

        // Device URI Templates
        public const string DeviceEventPathTemplate = "/devices/{0}/messages/events";

        public const string ModuleEventPathTemplate = "/devices/{0}/modules/{1}/messages/events";
        public const string DeviceBoundPathTemplate = "/devices/{0}/messages/deviceBound";
        public const string ModuleBoundPathTemplate = "/devices/{0}/modules/{1}/messages/deviceBound";
        public const string DeviceMethodPathTemplate = "/devices/{0}/methods/deviceBound";
        public const string ModuleMethodPathTemplate = "/devices/{0}/modules/{1}/methods/deviceBound";
        public const string DeviceTwinPathTemplate = "/devices/{0}/twin";
        public const string ModuleTwinPathTemplate = "/devices/{0}/modules/{1}/twin";
        public const string BlobUploadStatusPathTemplate = "/devices/{0}/files/";
        public const string BlobUploadPathTemplate = "/devices/{0}/files";
        public const string DeviceBoundPathCompleteTemplate = DeviceBoundPathTemplate + "/{1}";
        public const string DeviceBoundPathAbandonTemplate = DeviceBoundPathCompleteTemplate + "/abandon";
        public const string DeviceBoundPathRejectTemplate = DeviceBoundPathCompleteTemplate + "?reject";

        // IotHub provisioning terminal states (CSM/ARM)
        public const string ProvisioningStateSucceed = "Succeeded";

        public const string ProvisioningStateFailed = "Failed";
        public const string ProvisioningStateCanceled = "Canceled";

        public const string DeviceToCloudOperation = "d2c";
        public const string CloudToDeviceOperation = "c2d";

        public const string ApiVersionQueryParameterName = "api-version";

        // Service configurable parameters
        public const string PartitionCount = "PartitionCount";

        public const string TargetReplicaSetSize = "TargetReplicaSetSize";
        public const string MinReplicaSetSize = "MinReplicaSetSize";

        public const string ContentTypeHeaderName = "Content-Type";
        public const string ContentEncodingHeaderName = "Content-Encoding";
        public const string BatchedMessageContentType = "application/vnd.microsoft.iothub.json";

        public const string IotHubServiceNamePrefix = "iothub.";
        public const string IotHubSystemStoreServiceName = "iothub-systemstore";
        public const string AdminUriFormat = "/$admin/{0}?{1}";
        public const string DefaultConfigurationKey = "_default_config_key";

        // Security message constants
        public const string SecurityMessageInterfaceId = "urn:azureiot:Security:SecurityAgent:1";
    }
}
