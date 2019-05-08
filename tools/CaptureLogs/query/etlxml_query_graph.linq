<Query Kind="Statements" />

var xml = XElement.Load(@"<FILENAME>");
string entity = "<OBJECT_HASH>"; //e.g. ErrorDelegatingHandler#24710082

// All associate events
var associateEvents = from x in xml.Elements()
where x.Attribute("EventName").Value.Contains("Associate")
select x;

// 1. Add hash of all first==<current> -> second
var stack = new Stack<string>();
var connected = new HashSet<string>();
stack.Push(entity);
while (stack.Count != 0)
{
	string item = stack.Pop();
	connected.Add(item);
	
	var children = from x in associateEvents
	where x.Attribute("first").Value == item
	select x.Attribute("second").Value;
	
	foreach(string x in children)
	{
		if (x != "(null)") stack.Push(x);
	}
}

// 2. Add to the same hash all first=* -> second==<current>
stack.Push(entity);
while (stack.Count != 0)
{
	string item = stack.Pop();
	connected.Add(item);

	var parents = from x in associateEvents
				   where x.Attribute("second").Value == item
				   select x.Attribute("first").Value;

	foreach (string x in parents)
	{
		if (x != "(null)") stack.Push(x);
	}
}

connected.Dump("Associations");

var skipAttrib = new HashSet<string>();
skipAttrib.Add("EventName");
skipAttrib.Add("TimeMsec");
skipAttrib.Add("ThreadID");
skipAttrib.Add("ProcessName");
skipAttrib.Add("FormattedMessage");

string GetAttributes(XElement x)
{
	var sb = new StringBuilder();
	foreach (var attrib in x.Attributes())
	{
		if (!skipAttrib.Contains(attrib.Name.LocalName))
		{
			sb.AppendFormat("{0}, ", attrib.Value);
		}
	}
	
	return sb.ToString();
}

// 3. Display all related events
var relatedEvents = from x in xml.Elements()
where connected.Contains(x.Attribute("thisOrContextObject")?.Value)
					select $"{x.Attribute("TimeMsec")?.Value} [{x.Attribute("EventName")?.Value.Replace('/', '-')}] {GetAttributes(x)}";

relatedEvents.Dump("Events");