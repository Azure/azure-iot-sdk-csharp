# Transmit ETL
A single purpose tool that will send any ETL traces to an Application Insights instance.

## Overview
The tool was written to help get ETL traces from a remote machine to Application Insights without having to add instrumentation or store ETL log files on the remote machine. To do this we use the [Microsoft.Diagnostics.Tracing.TraceEvent](https://github.com/microsoft/perfview/blob/7bc1b55ebf6773f8afcdf46a96d2e9ccc763aeee/documentation/TraceEvent/TraceEventLibrary.md) library to stream the real time session that is created with [logman](https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/logman-create-trace).

## Operation
There are two required command line parameters that set the session name and connection string. They are `--sessionname` and `--connectionstring` respectively. 

There are options to set the heartbeat interval called `--heartbeatinterval` that sets the amount of time between heartbeats for the application.

There are also options to set the offline storage usd by Application Insights `--offlinestore` and `--maxstoresizemb` that sets the path and maximum size on disk the persistant storage will take.

### What is collected?
The Azure IoT SDK emits data from event sources and can contain your event hub endpoint name. 

**Example**
```
TransmitETL --sessionname <<SESSIONNAME>> --connectionstring <<APPLICATIONINSIGHTS CONNECTION STRING>>
```

**Standard Output**
```
> TransmitETL.exe

TransmitETL 1.0.0.0
Copyright c  2021

ERROR(S):
  Required option 'sessionname' is missing.
  Required option 'connectionstring' is missing.

  --sessionname          Required. The trace session to attach to.

  --connectionstring     Required. The Application Insights connection string.

  --offlinestore         (Default: .\offlinestore) Sets the directory for the Application Insights telemetry channel to store telemetry if the device is offline.

  --maxstoresizemb       (Default: 10) Sets the maximum store size in MB for telemetry that is persisted if the device is offline.

  --heartbeatinterval    (Default: 300) The interval in seconds to send the heartbeat.

  --help                 Display this help screen.

  --version              Display version information.
```

## Creating an Application Insights instance
Follow [these instructions](https://docs.microsoft.com/en-us/azure/azure-monitor/app/create-new-resource) on creating an Application Insights instance and get the [connection string](https://docs.microsoft.com/en-us/azure/azure-monitor/app/sdk-connection-string?tabs=net).

You will pass this connection string into the application

**Example connection string**
```
InstrumentationKey=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee;IngestionEndpoint=https://westus2-2.in.applicationinsights.azure.com/
```

## Logman Trace

The tool will need to have a logman trace running before it starts. Follow the instructions in the [README](../README.md) to get the correct provider files.

**Example logman command**
```
logman create trace IotTrace -rt -pf .\iot_providers.txt
```

## Additional Configuration

### Heartbeat
The tool will send a heartbeat at the specified interval (default: 300s) to application insights. You can use this heartbeat to determine if the tool is running and if there are events being sent. For example you can run the following query to see if the application is alive in Application Insights.

If you need more granular reporting you can set the heartbeat as low as 1s. You can also set the interval to 0 to disable the heartbeat, however this is not recommended.

### Offline Telemetry Store
Application insights uses the []

## Errors

### Logman Session Not Started

If the session is not started you will see the following error message. From an elevated command prompt you will need to start the logman trace. Or you can use the Performance Collector screen.

```
> TransmitETL.exe --sessionname IotTrace --connectionstring InstrumentationKey=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee

Using session: IotTrace
Using Application Insights connection string: InstrumentationKey=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
Using heartbeat interval: 300s
Creating session listener.
Heartbeat, sent 0 events
Creating session parser.
Starting session processing.
Error while processing event. See exception for more details.
The instance name passed was not recognized as valid by a WMI data provider. (Exception from HRESULT: 0x80071069)
Process is exiting with code 1.
```

### Invalid Logman Session Name

If the session is not started you will see the following error message. Check the session name and retry the command.

```
> TransmitETL.exe --sessionname invalid --connectionstring InstrumentationKey=a

Using session: invalid
Using Application Insights connection string: InstrumentationKey=a
Using heartbeat interval: 300s
Creating session listener.
Heartbeat, sent 0 events
Creating session parser.
Starting session processing.
Error while processing event. See exception for more details.
The instance name passed was not recognized as valid by a WMI data provider. (Exception from HRESULT: 0x80071069)
Process is exiting with code 1.
```

### Invalid Connection String

At a minimum you will need to specify a connection string with the format `InstrumentationKey=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee`.

```
TransmitETL.exe --sessionname invalid --connectionstring invalidstring

Using session: invalid
Using Application Insights connection string: invalidstring
Using heartbeat interval: 300s
Error creating the Application Insights instance. See exception for more details.
There was an error parsing the Connection String: Input contains invalid delimiters and cannot be parsed. Expected example: 'key1=value1;key2=value2;key3=value3'.
Process is exiting with code 1.
```