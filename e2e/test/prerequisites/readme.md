# Azure IoT C# End-to-end test prerequisites

The E2E tests require Azure services set up and configured with test devices (IoT Hub, Provisioning).
Please copy the iot_config.* scripts to a secure location, remove the `_template` suffix and follow the comments to fill in the missing information.

To generate required test certificates you can use the tools available [here](https://github.com/Azure/azure-iot-sdk-c/blob/master/tools/CACertificates/CACertificateOverview.md).

The tests require the following docker containers to run locally:

### TPM Simulator

Docker image: https://hub.docker.com/r/aziotbld/testtpm/

```Shell
docker pull aziotbld/testtpm
docker run -d --restart unless-stopped --name azure-iot-tpmsim -p 127.0.0.1:2321:2321 -p 127.0.0.1:2322:2322 aziotbld/testtpm
```

Alternatives:

 - Stand-alone executable for Windows: https://www.microsoft.com/en-us/download/details.aspx?id=52507

### Proxy Server

Docker image: https://hub.docker.com/r/aziotbld/testproxy/

```Shell
docker pull aziotbld/testproxy
docker run -d --restart unless-stopped --name azure-iot-tinyproxy -p 127.0.0.1:8888:8888 aziotbld/testproxy
```

Alternatives:

- TinyProxy for Linux/Mac: https://tinyproxy.github.io/
- Squid for Windows: https://chocolatey.org/packages/squid

### Far away hub

In order to test reprovisioning, your dps instance under test must be linked to two different iot hubs. The first hub should already be set under the
IOTHUB_CONN_STRING_CSHARP environment variable, and the other linked hub should be set as the value to the FAR_AWAY_IOTHUB_HOSTNAME environment variable

While the name of this variable indicates that the hub is far away, the tests currently do not require the hub to be far away. The tests
only require it to be a separate hub from the IOTHUB_CONN_STRING_CSHARP hub. It can be deployed to any region.

### Custom allocation policy webhook

Follow these instructions to setup a custom allocation webhook to test against:
https://docs.microsoft.com/en-us/azure/iot-dps/how-to-use-custom-allocation-policies

For the tests currently in this repo, your azure fucntion, which picks what iot hub to provision to, should always return the iot hub with the
longest hostname. Not all custom allocation policies need to work this way, but this is an easy-to-test functionality.

In this folder, there is a run.csx file that is the code the azure function should run in order to achieve this behavior.

The actual webhook url can be found through the azure portal, under your function, where there is a "Get Function URL" button that gives you a url
such as: "https://someazurefunction.azurewebsites.net/api/SomeTriggerName?code=XXXXXXXX" and this is the value that should be configured to the
CUSTOM_ALLOCATION_POLICY_WEBHOOK environment name

### Azure Security Center for IoT Security Message E2E Tests

The Azure Security Center for IoT Security Message E2E tests validates that messages marked with SetAsSecurityMessage and have Azure Security Center for IoT security message payload are sent correctly and ingested to the customer Log Analytics workspace.
For more information about Azure Security Center for IoT please visit: [Azure Security Center for IoT architecture](https://docs.microsoft.com/en-us/azure/asc-for-iot/architecture)

Important Note:

Azure Security Center for IoT is currently available in limited regions.
Please make sure the tested IoT Hub is in the supported regions

Test Flow:

- Generate fake message payload that complies with Azure Security Center for IoT security message schemas. reference: [Send security messages SDK](https://docs.microsoft.com/en-us/azure/asc-for-iot/how-to-send-security-messages)
- Mark the message as a security message with the method SetAsSecurityMessage
- Send the message in one of the supported Device SDK protocols
- Validate the message has arrived and ingested to the customer Log Analytics workspace

Prerequisites:

- Log Analytics workspace - Where Azure Security Center for IoT stores its data.
- Enable Azure Security Center for IoT - Azure Security Center for IoT should be enabled on the hub found in `Configuration.IoTHub` with the feature “Store raw device security events in LogAnalytics.” set to on, for onboarding instructions, please see: [ASC for IoT Quickstart](https://docs.microsoft.com/en-us/azure/asc-for-iot/quickstart-onboard-iot-hub)
- Azure Active Directory application with a reader role on the Log Analytics workspace - The tests uses Azure Active Directory Application to authenticate against the Log Analytics workspace. The service principal created by the Active Directory application must be assigned with a reader role on the Log Analytics workspace. Follow this link for instructions [Creating Azure Active Directory application and a service principal](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#assign-the-application-to-a-role)
- Self signed certificate - for authenticating with the Active Directory app

Note on Central US EUAP region:

Azure Security Center for IoT UI is currently not available in Central US EUAP.
To enable Azure Security Center for IoT in this region, please use the follwoing REST command:

```Shell
URL: https://management.azure.com/subscriptions/<SubscriptionID>/resourceGroups/<ResourceGroup>/providers/Microsoft.Security/IoTSecuritySolutions/<SecuritySolutionName>?api-version=2017-08-01-preview
Method: PUT
Headers:
Content-Type - application/json
Authorization - bearer token
Body:
{   "location": "North Europe",
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
- `LA_AAD_APP_CERT_BASE64` – The certificate for authenticating with the given app
- `LA_WORKSPACE_ID` – The Log Analytics workspace Id of the Log Analytics workspace that connected to the ASC for IoT security solution
