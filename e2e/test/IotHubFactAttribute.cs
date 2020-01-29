using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class IotHubFactAttribute : FactAttribute
    {
        public IotHubFactAttribute()
        {
            string runProvisioningTests = Configuration.GetValue("RUN_IOTHUB_TESTS", "True");
            if (!runProvisioningTests.Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                Skip = "Ignoring IotHub Test because RUN_IOTHUB_TESTS environment variable was set to " +
                    "a value other than \"True\"";
            }
        }
    }
}
