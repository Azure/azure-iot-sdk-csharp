using System;

namespace Microsoft.Azure.Devices.Client.Test
{
	internal class MSTestIgnoreAttribute : NUnit.Framework.IgnoreAttribute
	{
		public MSTestIgnoreAttribute() : base("") { }
	}
}