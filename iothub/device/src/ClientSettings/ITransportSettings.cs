// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

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
        /// The web proxy that will be used to connect to IoT hub using a web socket connection for AMQP, MQTT, or when using the
        /// HTTP protocol.
        /// </summary>
        /// <value>
        /// An instance of a class that implements <see cref="IWebProxy"/>.
        /// </value>
        /// <remarks>
        /// This setting will only be used when the client connects over web sockets or HTTPS.
        /// </remarks>
        /// <example>
        /// To set a proxy you must instantiate an instance of the <see cref="WebProxy"/> class--or any class that derives from
        /// <see cref="IWebProxy"/>. The snippet below shows a method that returns a device using a proxy that connects to localhost
        /// on port 8888.
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
        ///         return DeviceClient.CreateFromConnectionString("a connection string", new ITransportSettings[] { transportSettings });
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
