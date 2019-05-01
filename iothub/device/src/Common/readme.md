# Azure IoT C# Device Event Counter

The device event counter is designed to tracking device components life cycle events.

### What will be collected

No other infomation will be collected but status change counts, incluing counts of device client instance creation/dispose, AMQP unit(transport layer) creation/dispose, AMQP connection establish/disconnection, AMQP session establish/disconnection,
AMQP authentication refresher creation/dispose and AMQP authentication token refreshes.

### Enable ETW Event Counter Logger

Event counter could be captured to event tracing. It could be enabled during runtime but there is performance impact.

1. Download Perfview https://github.com/Microsoft/perfview/blob/master/documentation/Downloading.md To start collecting, please use following command on Command Prompt. Feel free to change EventCounterIntervalSec to what fits your environment.
```
PerfView /onlyProviders=*Microsoft-Azure-Devices-Device-Client-Event-Counters:EventCounterIntervalSec=1 collect
```
2. Stop collecting by clicking "stop collection" button. A file named 'PerfViewData.etl.zip' should be generated and shown on the left panel. Double click it and there double click 'events'. There will be a new window opened. Change 'MaxSet' to bigger value if the data set is huge. Sellect 'Microsoft-Azure-Devices-Device-Client-Event-Counters/EventCounters' on left panel and press F5 key. It will show events on right side. Right click the menu, and save to XML foramt.
3. To view event graph:
	a.Download LinQPad https://www.linqpad.net/download.aspx 
	b. Add following code in MyExtensions
```
public static class MyExtensions
{
	private const string s_eventPrefix = "<Event EventName=\"Microsoft-Azure-Devices-Device-Client-Event-Counters/EventCounters\"";
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
