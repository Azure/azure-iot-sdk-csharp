// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device onboarding.
    /// </summary>
    public class DeviceOnboardingResult
    {
        /// <summary>
        /// Initializes a new instance of the DeviceOnboardingResult class.
        /// </summary>
        public DeviceOnboardingResult(string operationId = default, Device result = default)
        {
            Id = operationId;
            Result = result;
        }

        /// <summary>
        /// Operation ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Resulting device details
        /// </summary>
        public Device Result { get; set; }
    }

    /// <summary> The device object. </summary>
    public class Device
    {
        /// <summary> Initializes a new instance of Device. </summary>
        /// <param name="registrationId"> The registrationId. </param>
        /// <param name="onboardingStatus"> The status of the onboarding process. </param>
        /// <param name="metadata"> Metadata associated with the device </param>
        public Device(string registrationId, string onboardingStatus, ResponseMetadata metadata)
        {
            RegistrationId = registrationId;
            OnboardingStatus = onboardingStatus;
            Metadata = metadata;
        }

        /// <summary> The registrationId. </summary>
        public string RegistrationId { get; }

        /// <summary> The status of the onboarding process.Possible values include:
        /// 'InProgress', 'Succeeded', 'Failed', 'Canceled'
        /// </summary>
        public string OnboardingStatus { get; }
        /// <summary>
        /// Metadata associated with the registered device
        /// </summary>
        public ResponseMetadata Metadata { get; }
    }

    /// <summary>
    /// Base response metadata class
    /// </summary>
    public class ResponseMetadata
    {

    }


    /// <summary>
    /// Object representing metadata associated with a hybrid compute machine
    /// </summary>
    public class HybridComputeMachine : ResponseMetadata 
    {
        /// <summary>
        /// Initializes a new instance of the HybridComputeMachineResponse
        /// class.
        /// </summary>
        /// <param name="resourceId">The device's Azure Resource Manager
        /// resourceId.</param>
        /// <param name="location">The Azure region to which the device
        /// belongs.</param>
        /// <param name="tenantId">The Azure Tenant associated with the
        /// device.</param>
        /// <param name="arcVirtualMachineId">The unique identifier of the
        /// Azure Arc Virtual Machine.</param>
        public HybridComputeMachine(string resourceId, string location, string tenantId, string arcVirtualMachineId)
        {
            ResourceId = resourceId;
            Location = location;
            TenantId = tenantId;
            ArcVirtualMachineId = arcVirtualMachineId;
        }

        /// <summary>
        /// Gets or sets the device's Azure Resource Manager resourceId.
        /// </summary>
        public string ResourceId { get; }

        /// <summary>
        /// Gets or sets the Azure region to which the device belongs.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets or sets the Azure Tenant associated with the device.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets or sets the unique identifier of the Azure Arc Virtual
        /// Machine.
        /// </summary>
        public string ArcVirtualMachineId { get; }
    }

    /// <summary>
    /// Object representing metadata associated with a device registry device
    /// </summary>
    public class DeviceRegistryDevice : ResponseMetadata 
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistryDeviceResponse
        /// class.
        /// </summary>
        /// <param name="assignedEndpoints">A list of endpoints assigned to the
        /// device. Required property for DeviceRegistryDevices.</param>
        public DeviceRegistryDevice(IList<Endpoint> assignedEndpoints)
        {
            AssignedEndpoints = assignedEndpoints;
        }

        /// <summary>
        /// Gets or sets a list of endpoints assigned to the device. Required
        /// property for DeviceRegistryDevices.
        /// </summary>
        public IList<Endpoint> AssignedEndpoints { get; set; }
    }

    /// <summary>
    /// Endpoint object.
    /// </summary>
    public partial class Endpoint
    {
        /// <summary>
        /// Initializes a new instance of the Endpoint class.
        /// </summary>
        /// <param name="hostname">The endpoint hostname.</param>
        /// <param name="name">The allocation group name.</param>
        public Endpoint(string hostname, string name)
        {
            Hostname = hostname;
            Name = name;
            EndpointType = "AzureEventGridBroker";
        }

        /// <summary>
        /// Gets or sets the endpoint hostname.
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// Gets or sets the allocation group name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The Endpoint type.
        /// </summary>
        public static string EndpointType { get; private set; }
    }
}