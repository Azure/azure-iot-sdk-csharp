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

string GetException(string thisOrContextObject, string message)
{
	if (thisOrContextObject.Contains("RetryDelegatingHandler")) return message.Split(':')[0];
	else if (thisOrContextObject.Contains("ErrorDelegatingHandler")) return message.Split(':')[1];
	else return message;
}

// All error events
var error = from x in xml.Elements()
			where x.Attribute("EventName").Value.Contains("Error")
			group x by GetException(x.Attribute("thisOrContextObject").Value, x.Attribute("message").Value) 
				into byExceptionType
				select byExceptionType;		// Replace by below to group exceptions by thread.
				//from threadIdGroup in 
				//	(from y in byExceptionType
	  			//	 group y by y.Attribute("ThreadID").Value)
				//group threadIdGroup by byExceptionType.Key;

Console.WriteLine($"{devices.Distinct().Count()} devices {modules.Distinct().Count()} modules");
Console.WriteLine($"{error.Distinct().Count()} error type(s)");

error.Distinct().Dump("Distinct errors", 5);

devices.Distinct().Dump("Devices");
modules.Distinct().Dump("Modules");