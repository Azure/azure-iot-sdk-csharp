# Azure IoT Hub Stress Test

Use `dotnet run` to display the help page for this tool.

## Prerequisites

##### Create an IoT Hub for performance testing.
##### Create one IoT Hub device with SAS token and one with X509 certificate authentication.

Fill in the following environment variables (see the e2e\test prerequisites for details):
```
IOTHUB_CONNECTION_STRING=
IOTHUB_DEVICE_CONN_STRING=
IOTHUB_DEVICE_CONN_STRING2=
IOTHUB_X509_DEVICE_PFX_CERTIFICATE=
```

##### Provision sufficient devices for the hub:

`dotnet run -- -a sas -n 100 -o out.csv -f generate_iothub_config`

Follow the instructions in the output for how to import the `devices.txt` file into your hub.
Note: You will need the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) and the [IoT extension for Azure CLI](https://github.com/Azure/azure-iot-cli-extension#installation).

## Examples

### Device to cloud test

#### Multiple devices communication

`dotnet run -- -o device.csv -t 60 -n 10 -f device_d2c`

#### Single device communicating

`dotnet run -- -o device.csv -t 60 -n 10 -f single_device_d2c`

### Cloud to device test

In two separate consoles:
```
dotnet run -- -o device.csv -t 60 -n 10 -f device_c2d
dotnet run -- -o service.csv -t 60 -n 10 -f service_c2d
```

### Direct method test

In two separate consoles:
```
dotnet run -- -o device.csv -t 60 -n 10 -f device_method
dotnet run -- -o service.csv -t 60 -n 10 -f service_method
```

## Combined tests
In three separate consoles:
```
dotnet run -- -o device.csv -t 60 -n 10 -f device_all
dotnet run -- -o service1.csv -t 60 -n 10 -f service_c2d
dotnet run -- -o service2.csv -t 60 -n 10 -f service_method
```
