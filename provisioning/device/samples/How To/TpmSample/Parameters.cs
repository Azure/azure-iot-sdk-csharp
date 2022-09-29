// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using System;
//using CommandLine;
//using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.Provisioning.Client.Samples
{
    // Commented out until we resolve plans for TPM support and library dependency
    ///// <summary>
    ///// The transport to use.
    ///// </summary>
    //public enum Transport
    //{
    //    /// <summary>
    //    /// MQTT 3.1.1.
    //    /// </summary>
    //    Mqtt,

    //    /// <summary>
    //    /// AMQP
    //    /// </summary>
    //    Amqp,
    //};

    ///// <summary>
    ///// Parameters for the application
    ///// </summary>
    //internal class Parameters
    //{
    //    [Option(
    //        'e',
    //        "GetTpmEndorsementKey",
    //        HelpText = "Gets the TPM endorsement key. Use this option by itself to get the EK needed to create a DPS individual enrollment.")]
    //    public bool GetTpmEndorsementKey { get; set; }

    //    [Option(
    //        's',
    //        "IdScope",
    //        HelpText = "The Id Scope of the DPS instance. For normal runs, this is required.")]
    //    public string IdScope { get; set; }

    //    [Option(
    //        'r',
    //        "RegistrationId",
    //        HelpText = "The registration Id from the individual enrollment. For normal runs, this is required.")]
    //    public string RegistrationId { get; set; }

    //    [Option(
    //        "GlobalDeviceEndpoint",
    //        Default = "global.azure-devices-provisioning.net",
    //        HelpText = "The global endpoint for devices to connect to.")]
    //    public string GlobalDeviceEndpoint { get; set; }

    //    [Option(
    //        "Transport",
    //        Default = Transport.Mqtt,
    //        HelpText = "The transport to use for the connection.")]
    //    public Transport Transport { get; set; }

    //    [Option(
    //        "TransportProtocol",
    //        Default = ProvisioningClientTransportProtocol.Tcp,
    //        HelpText = "The transport to use to communicate with the device provisioning instance.")]
    //    public ProvisioningClientTransportProtocol TransportProtocol { get; set; }

    //    internal ProvisioningClientOptions GetClientOptions()
    //    {
    //        return Transport switch
    //        {
    //            Transport.Mqtt => new ProvisioningClientOptions(new ProvisioningClientMqttSettings(TransportProtocol)),
    //            Transport.Amqp => new ProvisioningClientOptions(new ProvisioningClientAmqpSettings(TransportProtocol)),
    //            _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
    //        };
    //    }

    //    internal IotHubClientTransportSettings GetHubTransportSettings()
    //    {
    //        IotHubClientTransportProtocol protocol = TransportProtocol == ProvisioningClientTransportProtocol.Tcp
    //            ? IotHubClientTransportProtocol.Tcp
    //            : IotHubClientTransportProtocol.WebSocket;

    //        return Transport switch
    //        {
    //            Transport.Mqtt => new IotHubClientMqttSettings(protocol),
    //            Transport.Amqp => new IotHubClientAmqpSettings(protocol),
    //            _ => throw new NotSupportedException($"Unsupported transport type {Transport}/{TransportProtocol}"),
    //        };
    //    }
    //}
}
