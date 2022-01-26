using System;

namespace Microsoft.Azure.Devices.E2ETests.provisioning
{
    public class ConnectionStringParser
    {
        public string ProvisioningHostName { get; private set; }

        public string DeviceId { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessKeyName { get; private set; }

        public ConnectionStringParser(string connectionString)
        {
            string[] parts = connectionString.Split(';');
            foreach (string part in parts)
            {
                string[] tv = part.Split('=');

                switch (tv[0].ToUpperInvariant())
                {
                    case "HOSTNAME":
                        ProvisioningHostName = part.Substring("HOSTNAME=".Length);
                        break;

                    case "SHAREDACCESSKEY":
                        SharedAccessKey = part.Substring("SHAREDACCESSKEY=".Length);
                        break;

                    case "DEVICEID":
                        DeviceId = part.Substring("DEVICEID=".Length);
                        break;

                    case "SHAREDACCESSKEYNAME":
                        SharedAccessKeyName = part.Substring("SHAREDACCESSKEYNAME=".Length);
                        break;

                    default:
                        throw new NotSupportedException("Unrecognized tag found in test ConnectionString.");
                }
            }
        }
    }
}
