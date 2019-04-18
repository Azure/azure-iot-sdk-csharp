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

Prerequisites:

- The IoT hub configured in `Configuration.IoTHub` must be onboarded to ASC for IoT security, with the feature “Store raw device security events in LogAnalytics.” set to on.
- The tests authenticate with OMS with an Azure Active Directory app, thus an AAD app with a reader role on the OMS workspace must be created for the tests.

Test configuration:

- `OMS_AAD_TENANT` – The AAD tenant GUID of the OMS workspace Active Directory
- `OMS_AAD_APP_ID` – The application id of the AAD app that will be used to authenticate with oms
- `OMS_AAD_APP_KEY` – The key for the given app
- `OMS_WORKSPACE_ID` – The OMS workspace Id of the OMS workspace that connected to the ASC for IoT security solution