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