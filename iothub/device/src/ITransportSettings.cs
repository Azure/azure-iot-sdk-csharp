// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Interface used to define various transport-specific settings for DeviceClient and ModuleClient.
    /// </summary>
    public interface ITransportSettings
    {
        /// <summary>
        /// Returns the transport type of the TransportSettings object.
        /// </summary>
        /// <returns>The TransportType</returns>
        TransportType GetTransportType();

        /// <summary>
        /// The time to wait for a receive operation.
        /// </summary>
        TimeSpan DefaultReceiveTimeout { get; }

        /// <summary>
        /// The web proxy that will be used to connect to IoT hub using a web socket connection for AMQP, MQTT, or when using the HTTP protocol.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// This setting will be used when the client attempts to connect over web sockets. For example, if the client attempts to connect to IoT hub using <see cref="TransportType.Amqp"/> or <see cref="TransportType.Mqtt"/> the client will first try over TCP. If that fails, the client will fall back to using web sockets and will use the proxy setting. The setting will also be used when <see cref="TransportType.Amqp_WebSocket_Only"/>, <see cref="TransportType.Mqtt_WebSocket_Only"/>, or <see cref="TransportType.Http1"/> is specified. 
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost on port 8888.
        /// <c>
        /// static DeviceClient GetClientWithProxy()
        /// {
        ///     try
        ///     {
        ///         var proxyHost = "localhost";
        ///         var proxyPort = 8888;
        ///         // Specify the WebProxy to be used for the web socket connection
        ///         var transportSettings = new AmqpTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only)
        ///         {
        ///             Proxy = new WebProxy(proxyHost, proxyPort)
        ///         };
        ///         var deviceClient = DeviceClient.CreateFromConnectionString("a connection string", new ITransportSettings[] { transportSettings });
        ///         return deviceClient;
        ///     }
        ///     catch (Exception)
        ///     {
        ///         Console.WriteLine("Error creating client.");
        ///         throw;
        ///     }
        /// }
        /// </c>
        /// </example>
        IWebProxy Proxy { get; set; }
    }
}
