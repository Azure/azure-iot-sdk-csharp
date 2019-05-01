# Azure IoT C# Device Event Counter

The device event counter is designed to tracking device components life cycle events.

### What will be collected

No other infomation will be collected but status change counts, incluing counts of device client instance creation/dispose, AMQP unit(transport layer) creation/dispose, AMQP connection establish/disconnection, AMQP session establish/disconnection,
AMQP authentication refresher creation/dispose and AMQP authentication token refreshes.

### Enable ETW Event Counter Logger

Event counter could be captured to event tracing. It could be enabled during runtime but there is performance impact.

1. Download Perfview https://github.com/Microsoft/perfview/blob/master/documentation/Downloading.md To start collecting, please use following command on Command Prompt. Feel free to change EventCounterIntervalSec to what fits your environment.
```
PerfView /onlyProviders=*Microsoft-Azure-Devices-Shared-Device-Event-Counter:EventCounterIntervalSec=1 collect
```
2. Stop collecting by clicking "stop collection" button. A file named 'PerfViewData.etl.zip' should be generated and shown on the left panel. Double click it and there double click 'events'. There will be a new window opened. Change 'MaxSet' to bigger value if the data set is huge. Sellect 'Microsoft-Azure-Devices-Client-Logger-Event-Counter/EventCounters' on left panel and press F5 key. It will show events on right side. Right click the menu, and save to XML foramt.
3. To view event graph:
	a.Download LinQPad https://www.linqpad.net/download.aspx 
	b. Add following code in MyExtensions
```
public static class MyExtensions
{
	private const string s_eventPrefix = "<Event EventName=\"Microsoft-Azure-Devices-Shared-Device-Event-Counter/EventCounters\"";
	private const string s_timeOffsetPrefix = "TimeMsec=\"";
	private const string s_payloadPrefix = "Payload=\"";
	private const string s_namePrefix = "Name:\"";
	private const string s_countPrefix = "Count:";
	
	public static int GetValue(Dictionary<string, int> counts, string key)
	{
		counts.TryGetValue(key, out int count);
		return count;
	}

	public static EventCount ParseEventCount(string line, Dictionary<string, int> sums)
	{
		if (line.Length > 0)
		{
			// check event name
			int begin = line.IndexOf(s_eventPrefix);
			if (begin != -1)
			{
				begin += s_eventPrefix.Length;
				// Create event count
				EventCount eventCount = new EventCount();

				// begin of time offset
				begin = line.IndexOf(s_timeOffsetPrefix, begin) + s_timeOffsetPrefix.Length;
				// end of time offset
				int end = line.IndexOf("\"", begin);
				// round time offset to seconds
				eventCount.TimeOffset = (int)Math.Round(double.Parse(line.Substring(begin, end - begin)) / 1000, MidpointRounding.AwayFromZero);

				// begin of payload
				begin = line.IndexOf(s_payloadPrefix, end) + s_payloadPrefix.Length;

				// begin of name
				begin = line.IndexOf(s_namePrefix, begin) + s_namePrefix.Length;
				// end of name
				end = line.IndexOf("\",", begin);
				eventCount.Name = line.Substring(begin, end - begin);

				// begin of count
				begin = line.IndexOf(s_countPrefix, end) + s_countPrefix.Length;
				// end of count
				end = line.IndexOf(",", begin);
				eventCount.Count = MyExtensions.Sum(sums, eventCount.Name, int.Parse(line.Substring(begin, end - begin)));

				return eventCount;
			}
		}
		return null;
	}

	// Write custom extension methods here. They will be available to all queries.
	public static int Sum(Dictionary<string, int> sums, string key, int value)
	{
		sums.TryGetValue(key, out int sum);
		int count = sum + value;
		sums[key] = count;
		return count;
	}

	public static IEnumerable<EventCountGroup> GetEventCountEnumerable(string path) 
	{
		Dictionary<string, int> sums = new Dictionary<string, int>();
		string line;
		EventCountGroup eventCountGroup = new EventCountGroup();
		using(var reader = File.OpenText(path))
		{
			while((line = reader.ReadLine()) != null) 
			{
				EventCount eventCount = ParseEventCount(line, sums);
				if (eventCount != null) 
				{
					if (eventCountGroup.Counts.Count == 0)
					{
						eventCountGroup.TimeOffset = eventCount.TimeOffset;
						eventCountGroup.Counts[eventCount.Name] = eventCount.Count;
					}
					else if (eventCountGroup.TimeOffset == eventCount.TimeOffset)
					{
						eventCountGroup.Counts[eventCount.Name] = eventCount.Count;
					}
					else
					{
						// group finished and new group arrives
						var result = eventCountGroup;
						eventCountGroup = new EventCountGroup();
						eventCountGroup.TimeOffset = eventCount.TimeOffset;
						eventCountGroup.Counts[eventCount.Name] = eventCount.Count;
						yield return result;
					}
				}
			}
		}
		if (eventCountGroup.Counts.Count > 0)
		{
			yield return eventCountGroup;
		}
	}
}

// You can also define non-static classes, enums, etc.
public class EventCount
{
	public int TimeOffset { get; set; }
	public string Name { get; set; }
	public int Count { get; set; }
}

public class EventCountGroup
{
	public int TimeOffset { get; set; }
	public Dictionary<string, int> Counts { get; }

	public EventCountGroup()
	{
		Counts = new Dictionary<string, int>();
	}
}
```
	c. create new query as C# statement(s) and add following code.
```
string[] s_eventNames = {
	"Device-Client-Creation",
	"Device-Client-Disposal",
	"AMQP-Unit-Creation",
	"AMQP-Unit-Disposal",
	"AMQP-Connection-Establishment", 
	"AMQP-Connection-Disconnection", 
	"AMQP-Session-Establishment",
	"AMQP-Session-Disconnection",
	"AMQP-Token-Refresher-Initiation",
	"AMQP-Token-Refresher-Termination",
	"AMQP-Token-Refreshes",
};

var chart = MyExtensions.GetEventCountEnumerable(@"imported XML file path")
	.Chart(ecg => ecg.TimeOffset);

foreach (string name in s_eventNames)
{
	chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts, name), Util.SeriesType.Line, name);
}

chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts, s_eventNames[0])- MyExtensions.GetValue(ecg.Counts, s_eventNames[1]), Util.SeriesType.Line, "Device-Client-Addition");
chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts,s_eventNames[2]) - MyExtensions.GetValue(ecg.Counts, s_eventNames[3]), Util.SeriesType.Line, "AMQP-Unit-Addition");
chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts,s_eventNames[4]) - MyExtensions.GetValue(ecg.Counts,s_eventNames[5]), Util.SeriesType.Line, "AMQP-Connection-Addition");
chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts,s_eventNames[6]) - MyExtensions.GetValue(ecg.Counts,s_eventNames[7]), Util.SeriesType.Line, "AMQP-Session-Addition");
chart.AddYSeries(ecg => MyExtensions.GetValue(ecg.Counts,s_eventNames[8]) - MyExtensions.GetValue(ecg.Counts,s_eventNames[9]), Util.SeriesType.Line, "AMQP-Token-Refresher-Addition");

chart.Dump();
```
	d. Select Y series which you'd like to check.
	

### Customized device event monitor
We provide cusomized device event monitor with could be attached during runtime as well.

1. Implemet IDeviceEventMonitor interface.

```
public interface IDeviceEventMonitor
{
	/// <summary>
	/// When device event occurs 
	/// </summary>
	/// <param name="deviceEventName">Name of device event</param>
	void OnEvent(string deviceEventName);
}
``` 

2. Attach instance to Microsoft.Azure.Devices.Shared.DeviceEventMonitor. It returns list of device events which are been motitored. Only one IDeviceEventMonitor is allowed. Otherwise the former one will be kicked off.
```
IDeviceEventMonitor deviceEventMonitor = new YourCustomizedDeviceEventMonitor();
List<string> deviceEventNames = DeviceEventMonitor.Attach(deviceEventMonitor);
```

3. OnEvent function of YourCustomizedDeviceEventMonitor instance should be called once there is any related activity.
4. Once done, detach IDeviceEventMonitor from system.
```
DeviceEventMonitor.Detach();
```
5. Following is an example of customized device event monitor will store events in RAM and store to a templary file after done. The event is counted and grouped by secondes. Simply call Start/Stop function to attach/detach the monitor.

```
internal class MyDeviceEventMonitor : IDeviceEventMonitor
{
	private readonly Stopwatch _stopwatch;

	private bool _started;
	private List<string> _deviceEventNames;
	private BlockingCollection<DeviceEvent> _deviceEvents;

	internal MyDeviceEventMonitor()
	{
		_stopwatch = new Stopwatch();
	}

	internal void Start()
	{
		if (_started) return;

		_deviceEvents = new BlockingCollection<DeviceEvent>();
		_stopwatch.Start();
		_started = true;

		_deviceEventNames = DeviceEventMonitor.Attach(this);
	}

	internal void Stop()
	{
		if (_started)
		{
			DeviceEventMonitor.Detach();
			DumpAsCsvFile(_deviceEventNames, _deviceEvents.ToArray());
			_deviceEvents.Dispose();
			_stopwatch.Stop();
			_started = false;
		}
	}

	public void OnEvent(string deviceEventName)
	{
		_deviceEvents.Add(new DeviceEvent(_stopwatch.ElapsedMilliseconds, deviceEventName));
	}

	private static void DumpAsCsvFile(List<string> deviceEventNames, DeviceEvent[] deviceEvents)
	{
		int size = deviceEvents.Length;
		if (size == 0)
		{
			return;
		}

		Dictionary<string, int> eventCounts = new Dictionary<string, int>();

		string path = Path.GetTempFileName();
		using (StreamWriter sw = new StreamWriter(path))
		{
			sw.WriteLine($"Time,{string.Join(",", deviceEventNames)}");

			int timeOffset = -1;

			foreach (DeviceEvent deviceEvent in deviceEvents)
			{
				int offset = (deviceEvent._timeOffset + 999) / 1000;
				if (offset == timeOffset)
				{
					IncreseCount(eventCounts, deviceEvent._deviceEventName);
				}
				else
				{
					if (eventCounts.Count > 0)
					{
						// new time offset, write former counts to file 
						List<int> counts = new List<int>();
						foreach (string eventName in deviceEventNames)
						{
							eventCounts.TryGetValue(eventName, out int count);
							counts.Add(count);
						}
						string line = string.Join(",", counts);

						for (int time = timeOffset; time < offset; time++)
						{
							sw.WriteLine($"{time},{line}");
						}

					}

					// create new time offset counts
					timeOffset = offset;
					IncreseCount(eventCounts, deviceEvent._deviceEventName);
				}
			}

			// last time offset
			if (eventCounts.Count > 0)
			{
				List<int> counts = new List<int>();
				foreach (string eventName in deviceEventNames)
				{
					eventCounts.TryGetValue(eventName, out int count);
					counts.Add(count);
				}

				sw.WriteLine($"{timeOffset},{string.Join(",", counts)}");
				eventCounts.Clear();
			}
		}
		Console.WriteLine($"Device events are stored @ {path}");
	}

	private static void IncreseCount(Dictionary<string, int> counts, string key)
	{
		counts.TryGetValue(key, out int count);
		counts[key] = ++count;
	}

	class DeviceEvent
	{
		internal int _timeOffset;
		internal string  _deviceEventName;

		internal DeviceEvent(long timeOffset, string deviceEventName)
		{
			_timeOffset = (int) timeOffset;
			_deviceEventName = deviceEventName;
		}
	}
}
```

6. LinQPad script to create graph with sample device event monitor.
```
string path = @"tmap file path created, was printed on console";

string[] deviceEventNames = File.ReadLines(path).Take(1).Single().Split(',');

var deviceEvents = File.ReadLines(path).Skip(1).Select(line => line.Split(','));
	  
Console.WriteLine("X series with time offset in seconds.");
var chart = deviceEvents.Chart(deviceEvent => deviceEvent[0]);

Console.WriteLine($"Adding {deviceEventNames[1]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[1], Util.SeriesType.Line, deviceEventNames[1]);
Console.WriteLine($"Adding {deviceEventNames[2]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[2], Util.SeriesType.Line, deviceEventNames[2]);
Console.WriteLine($"Adding {deviceEventNames[3]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[3], Util.SeriesType.Line, deviceEventNames[3]);
Console.WriteLine($"Adding {deviceEventNames[4]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[4], Util.SeriesType.Line, deviceEventNames[4]);
Console.WriteLine($"Adding {deviceEventNames[5]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[5], Util.SeriesType.Line, deviceEventNames[5]);
Console.WriteLine($"Adding {deviceEventNames[6]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[6], Util.SeriesType.Line, deviceEventNames[6]);
Console.WriteLine($"Adding {deviceEventNames[7]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[7], Util.SeriesType.Line, deviceEventNames[7]);
Console.WriteLine($"Adding {deviceEventNames[8]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[8], Util.SeriesType.Line, deviceEventNames[8]);
Console.WriteLine($"Adding {deviceEventNames[9]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[9], Util.SeriesType.Line, deviceEventNames[9]);
Console.WriteLine($"Adding {deviceEventNames[10]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[10], Util.SeriesType.Line, deviceEventNames[10]);
Console.WriteLine($"Adding {deviceEventNames[11]} into chart");
chart.AddYSeries(deviceEvent => deviceEvent[11], Util.SeriesType.Line, deviceEventNames[11]);

Console.WriteLine("Adding Device-Client-Addition into chart");
chart.AddYSeries(deviceEvent => int.Parse(deviceEvent[1]) - int.Parse(deviceEvent[2]), Util.SeriesType.Line, "Device-Client-Addition");
Console.WriteLine("Adding AMQP-Unit-Addition into chart");
chart.AddYSeries(deviceEvent => int.Parse(deviceEvent[3]) - int.Parse(deviceEvent[4]), Util.SeriesType.Line, "AMQP-Unit-Addition");
Console.WriteLine("Adding AMQP-Connection-Addition into chart");
chart.AddYSeries(deviceEvent => int.Parse(deviceEvent[5]) - int.Parse(deviceEvent[6]), Util.SeriesType.Line, "AMQP-Connection-Addition");
Console.WriteLine("Adding AMQP-Session-Addition into chart");
chart.AddYSeries(deviceEvent => int.Parse(deviceEvent[7]) - int.Parse(deviceEvent[8]), Util.SeriesType.Line, "AMQP-Session-Addition");
Console.WriteLine("Adding AMQP-Token-Refresher-Addition into chart");
chart.AddYSeries(deviceEvent => int.Parse(deviceEvent[9]) - int.Parse(deviceEvent[10]), Util.SeriesType.Line, "AMQP-Token-Refresher-Addition");

chart.Dump();
``` 