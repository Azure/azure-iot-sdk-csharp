## Welcome to IotHubCommander 

Azure IoT Hub is a fully managed service that helps enable reliable and secure bi-directional communications between millions of devices and a solution back end. 
*IotHubCommander* is .Net Console Application, which supports most useful scenarios for testing IoT solutions, which leverage Microsoft Azure IoT Services. 

## Setup

1. Download setup.exe file from [here](https://github.com/daenetCorporation/azure-iot-sdk-csharp/blob/master/tools/IotHubCommander/IotHubCommander/publish/setup.exe) and install.
2. Write your event semicolon separated values in CSV file. i.e  

```csv
29.00;1987.12;3;1;  
31.21;1981.11;5;1;   
29.00;1987.12;3;0;  
31.21;1981.11;5;0;  
```

3. Write JSON formated Template file (Template.txt). i.e  
```JSON
{
   "Temp" : @1,
   "Humidity": @2,
   "Counter": @3,
   "SwitchValue": @4
}
```

4. Run Command Line Prompt and write commands for sending, receiving, IotHub migration etc.

## IoTHubCommander Scenarios

*IoTHubCommander* supports currently following scenarios:

* Device to Cloud
* Cloud to Device
* Cloud to Device event listener
* Feedback Receiver
* Read events from IotHub or EventHub
* IotHub Migration (preview)

### Device to Cloud 

This command is used to emulate device, which is sending events to IotHub. Events are stored in the CSV 
file serialized in JSON format defined by a template file.

*Example of CSV file, which contains sample data (separator is ';')*:
~~~csv
29.00;1987.12;3
31.21;1981.11;5	
29.00;1987.12;3
31.21;1981.11;5	
29.00;1987.12;3
31.21;1981.11;5	
~~~

*Example of JSON template file, which defines how to format events*:
~~~json
 {
   "Temp" : @1,
   "Humidity": @2,
   "Counter": @3,
}
~~~

Following example shows sending events Device to Cloud command:

~~~
IotHubCommander.exe --send=event --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvH/5HmRTkD8bR/YbEIU9IM= --cmdDelay=5 --eventFile=TextData1.csv --tempFile=JsonTemplate.txt
~~~

*--send*
Send event to Cloud from Device.

*--connStr*
Device connection string for sending events.

*--cmdDelay*
Delay time to listen.

*--eventFile*
Semicolon (";") separated CSV formated file path where events will be stored.

*--tempFile*
Json template text file. You can use this file to define how JSON formatted event looks like.

### Cloud to Device

By using this command you can send a list of events from Cloud to Device in predefined format.

Following example shows Cloud to Device command:

~~~
IotHubCommander.exe --send=Cloud --connStr=HostName=protadapter-testing.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=J95WJrRRbvZbSAV66CX/MKj66IJ7YnqvaqXSmIg5lY4= --deviceId=daenet-damir --eventFile=C:\GitHub\Azure-Iot-Sdks\tools\IotHubCommander\IotHubCommander\TextData2.csv --tempFile=C:\GitHub\Azure-Iot-Sdks\tools\IotHubCommander\IotHubCommander\JsonTemplate2.txt
~~~

*--send*
From where you want to send events in this case Cloud. 

*--connStr*
Connection string for sending event, Service connection string.

*--deviceId*
Your particular Device ID.

*--eventFile*
Semicolon (';') separated CSV formated file path with where events will be saved.

*--tempFile*
Json template text file path, You can use this file to define how JSON formatted event looks like.



### Cloud to Device event Listener

By using of this command you can emulate device, which is listening for Cloud-to-Device and automatically complete or abandon them.

Following example shows how to receive commands and automatically *abandons*:

~~~
IotHubCommander.exe --listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Abandon
~~~

Following example shows how to receive commands and automatically *complete*:

~~~
IotHubCommander.exe --listen=Device --connStr=HostName=something.azure-devices.net;DeviceId=123456;SharedAccessKey=2CFsCmqyHvHHmRTkD8bR/YbEIU9IM= --action=Commit
~~~


 *--listen*
 Listening as a Device from Cloud.

 *--connStr*
 Device Connection String for listening from Cloud.

 *--action*
 Commit the message Abandon, Commit or None. None is default command and will ask you for abandon or commit.



### Feedback Receiver
This command is used to get receiving of feedback, sent from Devices.

~~~
IotHubCommander.exe --listen=Device --connStr=HostName=something.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=LIFJbieayddyDz5W3s9mnxQCzr5458FDLnZ8o8BLVXXyW6Cc=
~~~

*--listen*
Receive feedback sent from Device.

*--connStr*
IotHub or EventHub connection string for getting feedback.

### Read events from IotHub or EventHub

By using of this command you can connect to IoTHub or EventHub and read all incoming events.
This is often used in none IoT scenarios to collect logging and trace events. For example, services like
Azure API Management writes events to EventHub.

Following example showing how to write command for reading events from IotHub or EventHub
~~~
IotHubCommander.exe --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IölkVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3h --consumerGroup=abc
IotHubCommander.exe --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3d --consumerGroup=abc
IotHubCommander.exe --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=-3s --consumerGroup=abc
IotHubCommander.exe --connectTo=EventHub --connStr=Endpoint=sb://sonethig-myevent-test.servicebus.windows.net/;SharedAccessKeyName=ReaderPolicy;SharedAccessKey=8AKA52124IVqj5eabciWz99UJWpDpQLQzwyLoWVKOTg=;EntityPath=abc --startTime=now --consumerGroup=abc
~~~


*--connectTo*
From where want to read events (IotHub or EventHub).

*--connStr*
IotHub or EventHub connection string, service connection string.

*--startTime*
Starting time to read events. It supports hour *(--startTime=-3h)*, day *(--startTime=-3d)*, second *(--startTime=-3s)* and Now *(--startTime=now)*

*--consumerGroup*
Consumer Group name, default is $Default.

### IotHub Migration (preview)
Exports all devices from origin IotHub and imports them to destination IotHub.

~~~
IotHubCommander.exe --migrateiothub=HostName=iothub-from.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=**** --to HostName=iothub-to.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=**** --acc=storageaccountname --key=**
~~~

*--migrateiothub*
The connection string of origin IotHub from where to migrate devices.

*--to*
The connection string of destination IotHub where devices will be migrated.

*--acc*
The name of storage account. Migration process uses the storage to export/import device data.

*--key*
The storage account key.

