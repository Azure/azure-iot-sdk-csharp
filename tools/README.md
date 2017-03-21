## Welcome to IotHubCommander   :)

Azure IoT Hub is a fully managed service that helps enable reliable and secure bi-directional communications between millions of devices and a solution back end. IotHubCommander is .Net Console Application, which sends event using some particular commands. IotHubCommander sends events -
* Device to Cloud
* Cloud to Device
* Read events from Cloud
* Getting feedback
* Read events from IotHub or EventHub

### Evironment Setup
* Download setup.exe file from [here](https://github.com/daenetCorporation/azure-iot-sdks/blob/develop/tools/IotHubCommander/IotHubCommander/publish/setup.exe) and install.
* Write your event semicolon separated values in CSV file. i.e  
```csv
29.00;1987.12;3;1;  
31.21;1981.11;5;1;   
29.00;1987.12;3;0;  
31.21;1981.11;5;0;  
```
* Write JSON formated Template file (Template.txt). i.e  
```JSON
{
   "Temp" : @1,
   "Humidity": @2,
   "Counter": @3,
   "SwitchValue": @4
}
```
* Write commands below for sending events.

### Send event from Device to Cloud 

To send event from Device to Cloud, run CommandLien promt and write command -
* --send=Event "for sending event to Cloud from Device".
* --connStr="Connection string for sending event, device connection string".
* --cmdDelay="Delay time to listen".
* --eventFile="csv formated file path with ";" separated value".
* --tempFile="Json template text file. You can use this file define how JSON formatted event looks like".

Example -
* IotHubCommander.exe --send=event --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvH/5HmRTkD8bR/YbEIU9IM= --cmdDelay=5 --eventFile=TextData1.csv --tempFile=JsonTemplate.txt

### Send Event Cloud to Device

To send event from Cloud to Device, run CommandLine promt and write command -
* --send=Cloud "for sending event to Device from Cloud".
* --connStr="Connection string for sending event, Service connection string".
* --deviceId="Device ID".
* --eventFile="csv formated file path with ";" separated value".
* --tempFile="Json template text file path, format of event".

Example - 
* IotHubCommander.exe --send=Cloud --connStr=HostName=protadapter-testing.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=J95WJrRRbvZbSAV66CX/MKj66IJ7YnqvaqXSmIg5lY4= --deviceId=daenet-damir --eventFile=C:\GitHub\Azure-Iot-Sdks\tools\IotHubCommander\IotHubCommander\TextData2.csv --tempFile=C:\GitHub\Azure-Iot-Sdks\tools\IotHubCommander\IotHubCommander\JsonTemplate2.txt

### Cloud to Device Listener

Write command below -
 * --listen=Device "for listening event".
 * --connStr="Connection string for reading event, Device connection string".
 * --action="Abandon, Commit or None", for abandon, Commit the message. None is default command and will ask you for abandon or commit.

Example -

* IotHubCommander.exe --listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Abandon
* IotHubCommander.exe --listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Commit


### Read events from IotHub or EventHub

To get event form IotHub or EventHub write command below -
* --connectTo="IotHub or EventHub for getting events".
* --connStr="IotHub or EventHub connection string, service connection string".
* --startTime="Starting time to read events".
* --consumerGroup="Consumer Group name, default is $Default".

Example
* --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IölkVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3h --consumerGroup=abc
* --connectTo=IotHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3d --consumerGroup=abc
* --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3s --consumerGroup=abc
* --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=now --consumerGroup=abc

