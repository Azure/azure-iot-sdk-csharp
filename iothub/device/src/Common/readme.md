# Azure IoT C# Device Event Counter

The device event counter is designed to tracking device components life cycle events. It could be enabled during runtime with minimised performance impact.

### What will be collected

No other infomation will be collected but status change counts, incluing counts of device client instance creation/dispose, AMQP unit(transport layer) creation/dispose, AMQP connection establish/disconnection, AMQP session establish/disconnection,
AMQP authentication refresher creation/dispose and AMQP authentication token refreshes.

### Enable Device Event Counter Logger

Event counter could be captured to event tracing. Download Perfview https://github.com/Microsoft/perfview/blob/master/documentation/Downloading.md 

1. To start collecting, please use following command on Command Prompt. Feel free to change EventCounterIntervalSec to what fits your environment.
```
PerfView /onlyProviders=*Microsoft-Azure-Devices-Shared-Device-Event-Counter:EventCounterIntervalSec=1 collect
```

2. Stop collecting by clicking "stop collection" button. A file named 'PerfViewData.etl.zip' should be generated and shown on the left panel. Double click it and there double click 'events'. There will be a new window opened. Change 'MaxSet' to bigger value if the data set is huge. Sellect 'Microsoft-Azure-Devices-Client-Logger-Event-Counter/EventCounters' on left panel and press F5 key. It will show events on right side. Right click the menu, it will show options to save to different file foramt or for Perfview 2.+, there is a new option 'Show EventCounter Graph'. It will generate a HTML page showing all different counts. It could zoom in and out.
