# Azure IoT C# Device Event Counter

The device event counter is designed to tracking device components life cycle events. It could be enabled during runtime with minimised performance impact.

### What will be collected

No other infomation will be collected but status change counts, incluing counts of device client instance creation/dispose, AMQP unit(transport layer) creation/dispose, AMQP connection establish/disconnection, AMQP session establish/disconnection,
AMQP authentication refresher creation/dispose and AMQP authentication token refreshes.

### Enable Device Event Counter Logger

Event counter could be captured to event tracing. Download Perfview https://github.com/Microsoft/perfview/blob/master/documentation/Downloading.md 

1. To start collecting, please use following command on Command Prompt. Feel free to change EventCounterIntervalSec to what fits your environment.
```
PerfView /onlyProviders=*Microsoft-Azure-Devices-Client-Logger-Event-Counter:EventCounterIntervalSec=1 collect
```

2. Add following code to your project. It will send event counter to event tracing. 
Report interval indicates how frequent counts are reported. Reporting will be terminated once cancellation token is cancelled. int the example, duration indicates how long the reporting will last. 
Meanwhile if you'd like to output the log to different place, a customized logger could be passed in.
There is a logger implementation build in which will redirect the counts to console output in CSV format. If the application is console application, it could work without any extra tool.
```
TimeSpan reportInterval = YourReportInterval;
TimeSpan reportDuration = YourReportDuration;
IEventCountLogger customizedLogger = ConsoleCounterLogger.GetInstance();
DeviceEventCounter.GetInstance().StartLoggerAsync(reportInterval, customizedLogger, new CancellationTokenSource(reportDuration).Token);
```

An example of 50K device pooling with 400 AMQP connections with 10 seconds report interval console output is as following:
```
Time,Device-Client-Creation,Device-Client-Dispose,AMQP-Unit-Creation,AMQP-Unit-Dispose,AMQP-Connection-Establish,AMQP-Connection-Disconnection,AMQP-Session-Establish,AMQP-Session-Disconnection,AMQP-Token-Refresher-Started,AMQP-Token-Refresher-Stopped,AMQP-Token-Refreshes
4/24/2019 1:40:53 PM,0,0,0,0,0,0,0,0,0,0,0
4/24/2019 1:41:03 PM,4557,0,4557,0,400,0,4528,0,400,400,400
4/24/2019 1:41:13 PM,7934,0,7934,0,400,0,7925,0,400,400,400
4/24/2019 1:41:23 PM,11575,0,11575,0,400,0,11540,0,400,400,400
4/24/2019 1:41:33 PM,18078,0,18077,0,400,0,18033,0,400,400,400
4/24/2019 1:41:43 PM,23915,0,23915,0,400,0,23899,0,400,400,400
4/24/2019 1:41:53 PM,29020,0,29019,0,400,0,28994,0,400,400,400
4/24/2019 1:42:03 PM,33529,0,33528,0,400,0,33512,0,400,400,400
4/24/2019 1:42:13 PM,37437,0,37436,0,400,0,37417,0,400,400,400
4/24/2019 1:42:23 PM,40828,0,40827,0,400,0,40824,0,400,400,400
4/24/2019 1:42:33 PM,44176,0,44175,0,400,0,44168,0,400,400,400
4/24/2019 1:42:43 PM,47130,0,47129,0,400,0,47120,0,400,400,400
4/24/2019 1:42:53 PM,50000,0,50000,0,400,0,50000,0,400,400,400
```
3. Stop collecting by clicking "stop collection" button. A file named 'PerfViewData.etl.zip' should be generated and shown on the left panel. Double click it and there double click 'events'. There will be a new window opened. Change 'MaxSet' to bigger value if the data set is huge. Sellect 'Microsoft-Azure-Devices-Client-Logger-Event-Counter/EventCounters' on left panel and press F5 key. It will show events on right side. Right click the menu, it will show options to save to different file foramt or for Perfview 2.+, there is a new option 'Show EventCounter Graph'. It will generate a HTML page showing all different counts. It could zoom in and out.
