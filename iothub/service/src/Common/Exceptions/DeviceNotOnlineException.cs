using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Common.Exceptions
{
    [Serializable]
    public class DeviceNotOnlineException : IotHubException
    {
        public DeviceNotOnlineException(string message) : base(message)
        {
        }

        public DeviceNotOnlineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
