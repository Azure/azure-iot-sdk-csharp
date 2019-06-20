using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client.Common
{
    /// <summary>
    /// Constants in AppContext that can be used to configure the behaviour or the Azure IoT SDK.
    /// </summary>
    public static class AppContextConstants
    {
        /// <summary>
        /// AppConfig switch (true/false) for disabling throwing ObjectDisposedException from DeviceClient.ReceiveAsync if DeviceClient is disposed.
        /// This reverts the functionality of DeviceClient.ReceiveAsync to how it was done on release version 1.18.0 and before, where the method just returned null.
        /// </summary>
        public const string DisableObjectDisposedExceptionForReceiveAsync = "Microsoft.Azure.Devices.Client.DisableObjectDisposedExceptionForReceiveAsync";
    }
}
