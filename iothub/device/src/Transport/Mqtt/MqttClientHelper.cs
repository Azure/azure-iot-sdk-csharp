using Microsoft.Azure.Devices.Client.Exceptions;
using System;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    class MqttClientHelper
    {
        public static Exception ToIotHubClientContract(Exception exception)
        {
            if (exception is TimeoutException)
            {
                return new IotHubCommunicationException(exception.Message, exception);
            }
            else if (exception is UnauthorizedAccessException)
            {
                return new UnauthorizedException(exception.Message, exception);
            }
            else
            {
                return exception;
            }
        }
    }
}
