<Query Kind="Statements" />

var xml = XElement.Load(@"<FILENAME>");
string entityType = "ErrorDelegatingHandler";

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

var relatedEvents = from x in xml.Elements()
where x.Attribute("thisOrContextObject")?.Value?.Contains(entityType) == true
					select $"{x.Attribute("TimeMsec")?.Value} [{x.Attribute("EventName")?.Value.Replace('/', '-')}] {GetAttributes(x)}";

relatedEvents.Dump("Events");
