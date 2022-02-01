using System;

namespace Microsoft.Azure.Devices.E2ETests.Provisioning
{
    internal class ConnectionStringParser
    {
        public string ProvisioningHostName { get; private set; }

        public string DeviceId { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessKeyName { get; private set; }

        public ConnectionStringParser(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(nameof(connectionString), "Parameter cannot be null, empty, or whitespace.");
            }

            string[] parts = connectionString.Split(';');

            foreach (string part in parts)
            {
                int separatorIndex = part.IndexOf('=');
                if (separatorIndex < 0)
                {
                    throw new ArgumentException($"Improperly formatted key/value pair: {part}.");
                }

                string key = part.Substring(0, separatorIndex);
                string value = part.Substring(separatorIndex + 1);

                switch (key.ToUpperInvariant())
                {
                    case "HOSTNAME":
                        ProvisioningHostName = value;
                        break;

                    case "SHAREDACCESSKEY":
                        SharedAccessKey = value;
                        break;

                    case "DEVICEID":
                        DeviceId = value;
                        break;

                    case "SHAREDACCESSKEYNAME":
                        SharedAccessKeyName = value;
                        break;

                    default:
                        throw new NotSupportedException($"Unrecognized tag found in parameter {nameof(connectionString)}.");
                }
            }
        }
    }
}
