# Azure IoT C# End-to-end test prerequisites

The E2E tests require Azure services set up and configured with test devices (IoT Hub, Provisioning).
Please copy the iot_config.* scripts to a secure location, remove the `_template` suffix and follow the comments to fill in the missing information.

To generate required test certificates you can use the tools available [here](https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md).

The tests require the following docker containers to run locally:

### TPM Simulator

Docker image: https://hub.docker.com/r/aziotbld/testtpm/

```
docker pull aziotbld/testtpm
docker run -d --restart unless-stopped --name azure-iot-tpmsim -p 127.0.0.1:2321:2321 -p 127.0.0.1:2322:2322 aziotbld/testtpm
```

Alternatives:
 - Stand-alone executable for Windows: https://www.microsoft.com/en-us/download/details.aspx?id=52507

### Proxy Server

Docker image: https://hub.docker.com/r/aziotbld/testproxy/

```
docker pull aziotbld/testproxy
docker run -d --restart unless-stopped --name azure-iot-tinyproxy -p 127.0.0.1:8888:8888 aziotbld/testproxy
```

Alternatives:
 - TinyProxy for Linux/Mac: https://tinyproxy.github.io/
 - Squid for Windows: https://chocolatey.org/packages/squid

### ASC for IoT Security Message E2E Tests

The ASC for IoT Security Message E2E tests validates that messages marked with SetAsSecurityMessage and have ASC for IoT security message payload are sent correctly and ingested to the customer Log Analytics workspace. 
For more information about ASC for IoT please visit: [ASC for IoT architecture](https://docs.microsoft.com/en-us/azure/asc-for-iot/architecture)

Important Note:

ASC for IoT is currently available in limited regions.
Please make sure the tested IoT Hub is in the supported regions

Test Flow:

- Generate fake message payload that complies with ASC for IoT security message schemas. reference: [Send security messages SDK](https://docs.microsoft.com/en-us/azure/asc-for-iot/how-to-send-security-messages)
- Mark the message as a security message with the method SetAsSecurityMessage
- Send the message in one of the supported Device SDK protocols
- Validate the message has arrived and ingested to the customer Log Analytics workspace

Prerequisites:

- Log Analytics workspace - Where ASC for IoT stores its data.
- Enable ASC for IoT - ASC for IoT should be enabled on the hub found in `Configuration.IoTHub` with the feature “Store raw device security events in LogAnalytics.” set to on, for onboarding instructions, please see: [ASC for IoT Quickstart](https://docs.microsoft.com/en-us/azure/asc-for-iot/quickstart-onboard-iot-hub) 
- Azure Active Directory application with a reader role on the Log Analytics workspace - The tests uses Azure Active Directory Application to authenticate against the Log Analytics workspace. The service principal created by the Active Directory application must be assigned with a reader role on the Log Analytics workspace. Follow this link for instructions [Creating Azure Active Directory application and a service principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#assign-the-application-to-a-role)

Note on Central US EUAP region:

ASC for IoT UI is currently not available in Central US EUAP.
To enable ASC for IoT in this region, please use the follwoing REST command:

```
URL: https://management.azure.com/subscriptions/<SubscriptionID>/resourceGroups/<ResourceGroup>/providers/Microsoft.Security/IoTSecuritySolutions/<SecuritySolutionName>?api-version=2017-08-01-preview
Method: PUT
Headers:
Content-Type - application/json
Authorization - bearer token
Body:
{   "location": "North Europe",
    "properties": {        
        "displayName": "<DisplayName>",
        "status": "Enabled",
        "export": ["RawEvents"],
        "disabledDataSources": [],
        "workspace": "<Log Analytics Resource ID>",
        "iotHubs": [
            "<IoT hub Resource ID>"
        ]
    }
}
```

Test configuration:

- `LA_AAD_TENANT` – The Azure Active Directory tenant, can be found [here](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Properties)  under Directory ID
- `LA_AAD_APP_ID` – The Azure Active Directory application ID. How to [Get application ID and authentication key](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#get-application-id-and-authentication-key)
- `LA_AAD_APP_KEY` – The key for the given app
- `LA_WORKSPACE_ID` – The Log Analytics workspace Id of the Log Analytics workspace that connected to the ASC for IoT security solution
