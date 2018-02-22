// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Connection status change reason supported by DeviceClient
    /// </summary>   
    public enum ConnectionStatusChangeReason
    {
        Connection_Ok,
        Expired_SAS_Token,
        Device_Disabled,
        Bad_Credential,
        Retry_Expired,
        No_Network,
        Communication_Error,
        Client_Close                    
    }
}
