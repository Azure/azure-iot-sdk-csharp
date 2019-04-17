<Query Kind="Statements" />

var xml = XElement.Load(@"<FILENAME>");

// All associate events
var associateEvents = from x in xml.Elements()
					  where x.Attribute("EventName").Value.Contains("Associate")
					  select x;

// Find all devices
var devices = from x in associateEvents
			  where x.Attribute("first").Value.Contains("DeviceClient")

			  select x.Attribute("first").Value;

// Find all modules
var modules = from x in associateEvents
			  where x.Attribute("first").Value.Contains("ModuleClient")
			  select x.Attribute("first").Value;

// All error events
var error = from x in xml.Elements()
			where x.Attribute("EventName").Value.Contains("Error")
			select x.Attribute("message").Value.Split(':')[1];

Console.WriteLine($"{devices.Distinct().Count()} devices {modules.Distinct().Count()} modules");
Console.WriteLine($"{error.Distinct().Count()} error type(s)");

error.Distinct().Dump("Distinct errors");

devices.Distinct().Dump("Devices");
modules.Distinct().Dump("Modules");
