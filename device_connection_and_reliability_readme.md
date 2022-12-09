# Azure IoT Device Client .NET SDK

## Device connection and messaging reliability

### Overview

In this document you will find information about:

- The connection authentication and renewal methods.
- The reconnection logic and retry policies.
- The timeout controls.

### Connection authentication

Authentication can be done using one of the following:

- [SAS tokens for the device](https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-sas?tabs=node#use-sas-tokens-as-a-device) - Using IoT hub [device shared access key](https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-sas?tabs=node#use-a-shared-access-policy-to-access-on-behalf-of-a-device) or [symmetric key](https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-sas?tabs=node#use-a-symmetric-key-in-the-identity-registry) from DPS identity registry
- [x509 certificate](https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-sas#supported-x509-certificates)  - Self signed or [CA-signed](https://docs.microsoft.com/azure/iot-hub/iot-hub-x509ca-overview)
- [TPM based authentication](https://azure.microsoft.com/blog/device-provisioning-identity-attestation-with-tpm/)

Samples:
- IoT hub device shared access key based authentication sample - [DeviceReconnectionSample](https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/iot-hub/Samples/device/DeviceReconnectionSample/DeviceReconnectionSample.cs#L102)
- Device provisioning service symmetric key based authentication sample - [ProvisioningDeviceClientSample](https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/provisioning/Samples/device/SymmetricKeySample/ProvisioningDeviceClientSample.cs#L62)
- x509 based authentication sample using CA-signed certificates - [X509DeviceCertWithChainSample](https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/iot-hub/Samples/device/X509DeviceCertWithChainSample/Program.cs#L43)
- TPM based authentication sample - [ProvisioningDeviceClientSample](https://github.com/Azure-Samples/azure-iot-samples-csharp/blob/main/provisioning/Samples/device/TpmSample/ProvisioningDeviceClientSample.cs#L49)

When using SAS tokens, authentication can be done by:

- Providing the shared access key of the IoT hub and letting the SDK create the SAS tokens by using one of the `CreateFromConnectionString` methods on the [DeviceClient](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceclient).

    If you choose this option, the SDK will create the SAS tokens and renew them before expiry. The default values for time-to-live and renewal buffer can be changed using the `ClientOptions` properties.

  - [SasTokenTimeToLive](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.clientoptions.sastokentimetolive): The suggested time-to-live value for tokens generated for SAS authenticated clients. Default value is 60 minutes.
  - [SasTokenRenewalBuffer](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.clientoptions.sastokenrenewalbuffer): The time buffer before expiry when the token should be renewed, expressed as a percentage of the time-to-live. Acceptable values lie between 0 and 100. Default value is 15%.  
  
  > Note:
  > 1. If the shared access policy name is not specified in the connection string, the audience for the token generation will be set by default to - `<iotHubHostName>/devices/<deviceId>`
  > 2. When authenticating a device using shared access key over AMQP, in-connection token refresh is supported.
  > 3. When authenticating a device using shared access key over MQTT, the connection will be briefly interrupted as part of the token refresh process.

- Providing only the shared access signature

    If you only provide the shared access signature, there will never be any renewal handled by the SDK.  
  
- Providing your own SAS token using [DeviceAuthenticationWithTokenRefresh](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithtokenrefresh)

    If you choose to use `DeviceAuthenticationWithTokenRefresh` to provide your own implementation of token generation, you can provide the time-to-live and time buffer before expiry through the `DeviceAuthenticationWithTokenRefresh` constructor. The `ClientOptions` only apply to other `IAunthenticationMethod` implementations.

When using x509 certificates, [DeviceAuthenticationWithX509Certificate](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithx509certificate) can be used. The client authentication will be valid until the certificate is valid. Any renewal will have to be done manually and the client needs to be recreated.

When using TPM based authentication, the [DeviceAuthenticationWithTpm](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithtpm) can be used. TPM based authentication will eventually generate a SAS token but is more secure than using the shared access key of the IoT hub to generate the token.

### Authentication methods implemented by the SDK

The different `IAuthenticationMethod` implementations provided by the SDK are:

- [DeviceAuthenticationWithRegistrySymmetricKey](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithregistrysymmetrickey) - Authentication method that uses the symmetric key associated with the device in the device registry.
- [DeviceAuthenticationWithSharedAccessPolicyKey](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithsharedaccesspolicykey) - Authentication method that uses a shared access policy key.
- [DeviceAuthenticationWithToken](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithtoken) - Authentication method that uses a shared access signature token.
- [DeviceAuthenticationWithTokenRefresh](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithtokenrefresh) - Abstract class that can be implemented to generate a shared access signature token and allows for token refresh.
- [DeviceAuthenticationWithTpm](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithtpm) - Authentication method that uses a shared access signature token generated using TPM and allows for token refresh.
- [DeviceAuthenticationWithX509Certificate](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.deviceauthenticationwithx509certificate) - Authentication method that uses a X.509 certificates.

### Connection retry logic

For both AMQP and MQTT, the SDK will try to reconnect anytime there is any network related disruption. The default retry policy does not have a time limit and will follow exponential back-off.

> Note: The default retry policy has support for jitter, which ensures that if you have N devices that disconnected at the same time, all of them won't start reconnecting with the same delay.

For more details on the default retry policy and how to override it, see [retry policy documentation](https://github.com/Azure/azure-iot-sdk-csharp/blob/main/iothub/device/devdoc/retrypolicy.md).

HTTP is a stateless protocol and will work whenever there is network connectivity.

### Timeout controls

There are different timeout values that can be configured for the `DeviceClient`/`ModuleClient` based on the protocol. These values are configuarable through the following transport settings that are passed while creating the client. Once the client is created, the settings cannot be changed. The client will need to be recreated with new settings to make changes.

AMQP timeout settings:

- [IdleTimeout](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.amqptransportsettings.idletimeout) - The interval that the client establishes with the service, for sending keep-alive pings. The default value is 2 minutes.
- [OperationTimeout](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.amqptransportsettings.operationtimeout) - The time to wait for any operation to complete. The default is 1 minute.
- [OpenTimeout](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.amqptransportsettings.opentimeout) - This value is not used (TODO: Confirm and update)

MQTT timeout settings:

- [ConnectArrivalTimeout](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.transport.mqtt.mqtttransportsettings.connectarrivaltimeout) - The time to wait for receiving an acknowledgment for a CONNECT packet. The default is 1 minute.
- [KeepAliveInSeconds](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.transport.mqtt.mqtttransportsettings.keepaliveinseconds) - The interval, in seconds, that the client establishes with the service, for sending keep-alive pings. The default value is 5 minutes. The client will send a ping request 4 times per keep-alive duration set. It will wait for 30 seconds for the ping response, else mark the connection as disconnected.
- [DeviceReceiveAckTimeout](https://docs.microsoft.com/dotnet/api/microsoft.azure.devices.client.transport.mqtt.mqtttransportsettings.devicereceiveacktimeout) -  The time a device will wait for an acknowledgment from service. The default is 5 minutes.
