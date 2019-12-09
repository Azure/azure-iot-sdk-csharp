### **_Device Explorer is deprecated as of 10/30/2019 and is being replaced by [Azure IOT Explorer][lnk-azure-iot-explorer] . [This](https://github.com/Azure/azure-iot-sdk-csharp/releases/tag/2019-9-11) is the last release of Device Explorer._**

# How to use Device Explorer for IoT Hub devices
This topic describes how to download, build and use the Device Explorer tool. You can use this tool to manage devices connecting to your IoT hub. For example, you can register a device with your IoT hub, monitor messages from your devices, and send messages to your devices.

Device Explorer runs on your local machine and connects to your IoT hub in Azure. It communicates with the following [IoT Hub endpoints][lnk-devguide]:

- *Device identity management* to provision and manage devices registtered with your IoT hub.
- *Receive device-to-cloud* to enable you to monitor messages sent from your device to your IoT hub.
- *Send cloud-to-device* to enable you to send messages to your devices from your IoT hub.

> **Note:** The Device Explorer utility only runs on Windows. On other platforms use the [iothub explorer][iothub-explorer] tool.

To learn more about IoT Hub, see [What is Azure IoT Hub?][what-is-iot-hub].

## Table of Contents
- [Getting Device Explorer](#download)
- [Configure an IoT Hub connection](#configure)
- [Manage devices](#managedevices)
  - [List registered devices](#listdevices)
  - [Create device](#createdevice)
  - [Update device](#updatedevice)
  - [Delete device](#deletedevice)
  - [SAS Token](#sastoken)
  - [Get device connection string or data](#getdatadevice)
- [Monitor device-to-cloud events](#monitor)
- [Send cloud-to-device messages](#send)
- [Call Method on device](#method)

<a name="download"/>

## Getting Device Explorer

You can either download a pre-built version of Device Explorer or build it yourself.

### Download a pre-built version of the Device Explorer application

A pre-built version of the Device Explorer application for Windows can be downloaded from https://aka.ms/aziotdevexp as well as the [Releases](https://github.com/Azure/azure-iot-sdk-csharp/releases) page. (Scroll down for __SetupDeviceExplorer.msi__). The default installation directory for this application is "C:\Program Files (x86)\Microsoft\DeviceExplorer". You might want to pin the DeviceExplorer.exe application to the taskbar for easier access.


### Build the Device Explorer application

To build Device Explorer yourself and generate the MSI, the following component needs to be added to Visual Studio:
[Microsoft Visual Studio 2017 Installer Projects][lnk-VS-Installer]
    You will need to add the highlighted key: Computer\HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\15.0\_**cbfbe12c**\_Config\MSBuild
    ![](./VS_installer_key.png)
      1. Add the node 15.0\_**[User ID]**\_Config like as shown below 15.0\_**cbfbe12c**\_Config.
      2. Add MSBuild folder (key) in it.
      3. In MSBuild, create new DWORD Value "EnableOutOfProcBuild" and set its value as 0.

Once installed, open the **tools\\DeviceExplorer\\DeviceExplorerWithInstaller.sln** file in your local copy of this repository in Visual Studio 2017. Then build and run the solution.

Alternatively run the following in the root of the repository: `jenkins\windows_csharp_deviceexplorer.cmd`

<a name="configure"/>

## Configure an IoT Hub connection


-  In the **Configuration** tab, add the connection string for your IoT Hub. For information about how to find this connection string, see the document [Setup your IoT Hub][setup-iothub]. Then click **Update**.

  ![](doc/media/device_explorer/iotgetstart1.png)

### Configure a Protocol Gateway

You only need to complete this step if you are using a Protocol Gateway (such as the [Microsoft Azure IoT Protocol Gateway][protocol-gateway]).

Add the address of your protocol gateway to the **Protocol Gateway HostName** field with the address of your protocol gateway before you try to get a device connection string on the **Management** tab. If you are running the protocol gateway locally on the same machine as Device Explorer, you can use **localhost** as the address, otherwise you will need the network address of the machine where you deployed the protocol gateway.

<a name="managedevices"/>

## Manage devices
-  Click the **Management** tab to manage the devices connected to the IoT hub.

  ![](doc/media/device_explorer/iotgetstart2.png)

<a name="listdevices"/>

-  Click **List** to query the list of registered devices with the connected IoT Hub.

<a name="createdevice"/>

### Create device

Creating a device adds device details to the device identity registry. IoT Hub uses this information to generate a device-specific connection string that enables the device to connect to the IoT hub.

-  On the **Management** tab, click **Create** to register a new device with that IoT Hub. The **Create Device** dialog appears. In the **Device ID** field, type a unique name for your device (or select **Auto Generate ID** to generate a unique ID instead). Then click **Create**.

  ![](doc/media/device_explorer/iotgetstart3.png)

-  A **Device Created** window appears, indicating that your device has been successfully registered with this IoT Hub.

  ![](doc/media/device_explorer/iotgetstart4.png)

<a name="updatedevice"/>

### Update device

- Click **Update** to change the primary or secondary key for the selected device. You will need to update your device to use any new keys you generate.


<a name="deletedevice"/>

### Delete device

- Click **Delete** to delete the selected devices from the connected IoT hub.

<a name="sastoken"/>

### SAS Token

- Click **SAS Token...** to generate a device specific connection string with SAS token.

<a name="getdatadevice"/>

### Get device connection string or configuration data

- Use the mouse right click for context menu for the selected device. The menu has the following options

  ![](doc/media/device_explorer/connstr.png)

<a name="monitor"/>

## Monitor device-to-cloud events

The following steps describe how to use Device Explorer to monitor device-to-cloud events sent from a sample application on a client to an IoT hub and use the **Device Explorer** utility to observe as the IoT hub receives the event data.

### Obtain the device connection string and setup monitoring

You can use one of the sample applications included in this repository ([azure-iot-sdks](https://github.com/Azure/azure-iot-sdks)) to send device-to-cloud events to your IoT Hub. Follow the [step by step tutorial to set up the sample application][sample-application-tutorial]. Use Device Explorer to [create a device](#createdevice) and generate a connection string.

-  You can obtain the IoT Hub connection string from Device Explorer. In the Management tab, right-click on the selected device and select "Copy connection string for selected device". The device connection string is silently copied to the clipboard.

> Note: Device Explorer requires an IoT hub connection string obtained from the Azure portal to enable it to connect to the hub. You can then use Device Explorer to generate device-specific connection strings that enable devices to communicate with the hub.

- Update the device connection string placeholder in the application you are using with the connection string from Device Explorer.

-  In the Device Explorer **Data** tab, select the device name you created from the drop-down list of device IDs and leave the other fields with their default values for now.

-  Click **Monitor**.

  ![](doc/media/device_explorer/iotgetstart5.png)

-  Device Explorer is now monitoring data sent from that device to the IoT hub.

### Run the sample application

Run your sample application. Device Explorer should now show that the IoT hub has successfully received device-to-cloud data from this device.

  ![](doc/media/device_explorer/iotgetstart7.png)

<a name="send"/>

## Send cloud-to-device messages

-  To verify that you can send cloud-to-device messages from the IoT hub to your device, go to the **Messages To Device** tab in Device Explorer.

-  Select the device you created.

-  Add some text to the **Message** field, then click **Send**.

  ![](doc/media/device_explorer/iotgetstart8.png)

-  You should be able to see the command received in the console window for the client sample application you are using.

  ![](doc/media/device_explorer/iotgetstart9.png)

>Note: You must be sure to use the correct format when you send a message to your device. For example, the **simplesample_amqp** sample for the C serializer library accepts the following JSON commands:

```
{"Name":"SetAirResistance","Parameters":{"Position":55}}
{"Name":"TurnFanOn","Parameters":{}}
```

<a name="method"/>

## Call Method on device

- To call Method on device, you can use this [sample application](../../iothub/device/samples/DeviceClientMethodSample) from IoT client sdk.

- Open this sample application in Visual Studio; Update device connection string in sample application.  Then hit F5 to run it.
 
- Go to **Call Method on Device" tab in Device Explorer.

- Input `WriteToConsole` in **Method name** field and `{ "MethodPayload": "hello world!" }` in **Method payload** field; then click **Call Method**.

  ![](doc/media/device_explorer/iotgetstart10.png)

- You should be able to see the **Method payload** message printed in the console window for the client sample application you are using.

  ![](doc/media/device_explorer/iotgetstart11.png)



[setup-iothub]: ../../doc/setup_iothub.md
[protocol-gateway]: https://github.com/Azure/azure-iot-protocol-gateway
[lnk-devguide]: https://azure.microsoft.com/documentation/articles/iot-hub-devguide/
[what-is-iot-hub]: https://azure.microsoft.com/documentation/articles/iot-hub-what-is-iot-hub/
[iothub-explorer]: https://github.com/Azure/iothub-explorer#iothub-explorer
[sample-application-tutorial]: ../../doc/get_started/arduinoide-arduino-wifi101-c.md
[lnk-VS-Installer]:https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.MicrosoftVisualStudio2017InstallerProjects
[lnk-Win-IoT-SDK]:https://marketplace.visualstudio.com/items?itemName=MicrosoftIoT.WindowsIoTCoreProjectTemplatesforVS15
[lnk-azure-iot-explorer]:https://github.com/Azure/azure-iot-explorer