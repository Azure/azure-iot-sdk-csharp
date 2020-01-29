using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class ProvisioningFactAttribute : FactAttribute
    {
        public ProvisioningFactAttribute()
        {
            string runProvisioningTests = Configuration.GetValue("RUN_PROVISIONING_TESTS", "True");
            if (!runProvisioningTests.Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                Skip = "Ignoring IotHub Test because RUN_PROVISIONING_TESTS environment variable was set to " +
                    "a value other than \"True\"";
            }
        }
    }
}
