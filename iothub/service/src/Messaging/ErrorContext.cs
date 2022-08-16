using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The IoT Hub level exception, if any IoT Hub level exception caused this connection loss.
        /// </summary>
        /// <remarks>
        /// For example, if the device does not exist.
        /// </remarks>
        /// <param name="iotHubException"></param>
        public ErrorContext(IotHubException iotHubException)
        {
            IotHubException = iotHubException;
        }

        /// <summary>
        /// The network level exception, if any network level exception caused this connection loss.
        /// </summary>
        /// <remarks>
        /// For example, if the device has no internet connection.
        /// </remarks>
        /// <param name="iOException"></param>
        public ErrorContext(IOException iOException)
        {
            IOException = iOException;
        }

        IotHubException IotHubException { get; }
        IOException IOException { get; }
    }
}
