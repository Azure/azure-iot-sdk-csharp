// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Connection status change reason supported by DeviceClient
    /// </summary>
    public enum ConnectionStatusChangeReason
    {
        /// <summary>
        /// This signifies that the client is connected, and ready to be used.
        /// <para>This connection status change reason is returned with a connection status of <see cref="ConnectionStatus.Connected"/>.</para>
        /// </summary>
        Connection_Ok,

        /// <summary>
        /// This signifies that the SAS token associated with the client has expired, and cannot be renewed.
        /// The supplied credentials need to be fixed before a connection can be established.
        /// <para>NOTE: This status change reason is currently not used in the client library.</para>
        /// </summary>
        Expired_SAS_Token,

        /// <summary>
        /// This signifies that the device/ module has been deleted or marked as disabled (on your hub instance).
        /// Fix the device/ module status in Azure before attempting to create the associated client instance.
        /// <para>This connection status change reason is returned with a connection status of <see cref="ConnectionStatus.Disconnected"/></para>
        /// </summary>
        Device_Disabled,

        /// <summary>
        /// This signifies that incorrect credentials were supplied to the client instance.
        /// The supplied credentials need to be fixed before a connection can be established.
        /// <para>This connection status change reason is returned with a connection status of <see cref="ConnectionStatus.Disconnected"/></para>
        /// </summary>
        Bad_Credential,

        /// <summary>
        /// This signifies that the client was disconnected due to a transient exception, but the retry policy expired before a connection could be re-established.
        /// If you want to perform more operations on the device client, you should <see cref="DeviceClient.Dispose()"/> and then re-initialize the client.
        /// <para>This connection status change reason is returned with a connection status of <see cref="ConnectionStatus.Disconnected"/></para>
        /// </summary>
        Retry_Expired,

        /// <summary>
        /// This signifies that the client was disconnected due to loss of network.
        /// <para>NOTE: This status change reason is currently not used in the client library.</para>
        /// </summary>
        No_Network,

        /// <summary>
        /// When returned with a connection status of <see cref="ConnectionStatus.Disconnected_Retrying"/>, this signifies that the client is trying to recover from a disconnect due to a transient exception.
        /// Do NOT close or open the client instance. Once the client successfully reports <see cref="ConnectionStatus.Connected"/>, you can resume operations on the client.
        /// <para>When returned with a connection status of <see cref="ConnectionStatus.Disconnected"/> signifies that client is disconnected due to a non-retryable exception.
        /// If you want to perform more operations on the device client, you should <see cref="DeviceClient.Dispose()"/> and then re-initialize the client.</para>
        /// </summary>
        Communication_Error,

        /// <summary>
        /// This signifies that the client has been closed gracefully.
        /// If you want to perform more operations on the device client, you should <see cref="DeviceClient.Dispose()"/> and then re-initialize the client.
        /// <para>This connection status change reason is returned with a connection status of <see cref="ConnectionStatus.Disabled"/> </para>
        /// </summary>
        Client_Close,
    }
}
