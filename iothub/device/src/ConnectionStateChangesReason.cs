// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Connection state change reason supported by the client.
    /// </summary>
    public enum ConnectionStateChangesReason
    {
        /// <summary>
        /// The client is connected, and ready to be used.
        /// <para>This is returned with a connection state of <see cref="ConnectionState.Connected"/>.</para>
        /// </summary>
        ConnectionOk,

        /// <summary>
        /// The SAS token associated with the client has expired, and cannot be renewed.
        /// The supplied credentials need to be fixed before a connection can be established.
        /// <para>NOTE: This is currently not used in the client library.</para>
        /// </summary>
        ExpiredSasToken,

        /// <summary>
        /// The device/ module has been deleted or marked as disabled (on your IoT hub instance).
        /// Fix the device/ module state in Azure before attempting to create the associated client instance.
        /// <para>This is returned with a connection state of <see cref="ConnectionState.Disconnected"/>.</para>
        /// </summary>
        DeviceDisabled,

        /// <summary>
        /// Incorrect credentials were supplied to the client instance.
        /// The supplied credentials need to be fixed before a connection can be established.
        /// <para>This is returned with a connection state of <see cref="ConnectionState.Disconnected"/>.</para>
        /// </summary>
        BadCredential,

        /// <summary>
        /// The client was disconnected due to a transient exception, but the retry policy expired before a connection
        /// could be re-established. If you want to perform more operations on the device client, one should call
        /// <see cref="IotHubDeviceClient.Dispose()"/> and then re-initialize the client.
        /// <para>This is returned with a connection state of <see cref="ConnectionState.Disconnected"/>.</para>
        /// </summary>
        RetryExpired,

        /// <summary>
        /// The client was disconnected due to loss of network.
        /// <para>NOTE: This is currently not used in the client library.</para>
        /// </summary>
        NoNetwork,

        /// <summary>
        /// This can be returned with either a connection state of <see cref="ConnectionState.DisconnectedRetrying"/>
        /// or <see cref="ConnectionState.Disconnected"/>.
        /// <para>When returned with a connection state of <see cref="ConnectionState.DisconnectedRetrying"/>,
        /// this signifies that the client is trying to recover from a disconnect due to a transient exception.
        /// Do NOT close or open the client instance. Once the client successfully reports <see cref="ConnectionState.Connected"/>,
        /// you can resume operations on the client.</para>
        /// <para>When returned with a connection state of <see cref="ConnectionState.Disconnected"/> signifies that
        /// client is disconnected due to a non-retryable exception.
        /// If you want to perform more operations on the device client, one should call <see cref="IotHubDeviceClient.Dispose()"/>
        /// and then re-initialize the client.</para>
        /// </summary>
        CommunicationError,

        /// <summary>
        /// The client has been closed gracefully.
        /// If you want to perform more operations on the device client, one should call <see cref="IotHubDeviceClient.Dispose()"/>
        /// and then re-initialize the client.
        /// <para>This is returned with a connection state of <see cref="ConnectionState.Disabled"/> </para>
        /// </summary>
        ClientClose,
    }
}
