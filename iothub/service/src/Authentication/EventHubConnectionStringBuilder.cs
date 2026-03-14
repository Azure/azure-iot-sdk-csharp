// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Amqp.Transport;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Gets an event hub-compatible connection string to be used with <see href="https://www.nuget.org/packages/Azure.Messaging.EventHubs"/>.
    /// </summary>
    internal static class EventHubConnectionStringBuilder
    {
        /// <summary>The regular expression used to parse the Event Hub name from the IoT hub redirection address.</summary>
        private static readonly Regex s_eventHubNameExpression = new(
            @":\d+\/(?<eventHubName>.*)\/\$management",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Requests connection string for the built-in Event Hubs messaging endpoint of the associated IoT hub.
        /// </summary>
        /// <param name="iotHubConnectionString">The connection string for the IoT hub instance to request the Event Hubs connection string from.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>A connection string which can be used to connect to the Event Hubs service and interact with the IoT hub messaging endpoint.</returns>
        /// <exception cref="InvalidOperationException">The Event Hubs host information was not returned by the IoT hub service.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-endpoints"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-amqp-support#receive-telemetry-messages-service-client"/>
        internal static async Task<string> GetEventHubCompatibleConnectionStringAsync(string iotHubConnectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(iotHubConnectionString))
            {
                throw new ArgumentException("The IoT hub connection string must be provided.", nameof(iotHubConnectionString));
            }

            // Parse the connection string into the necessary components, and ensure the information is available.

            IotHubConnectionString parsedConnectionString = IotHubConnectionStringParser.Parse(iotHubConnectionString);
            string iotHubName = parsedConnectionString.HostName?.Substring(0, parsedConnectionString.HostName.IndexOf('.'));

            if (string.IsNullOrEmpty(parsedConnectionString.HostName)
                || string.IsNullOrEmpty(parsedConnectionString.SharedAccessKeyName)
                || string.IsNullOrEmpty(parsedConnectionString.SharedAccessKey))
            {
                throw new ArgumentException("The IoT hub connection string is not valid; it must contain the host, shared access key, and shared access key name.", nameof(iotHubConnectionString));
            }

            if (string.IsNullOrEmpty(iotHubName))
            {
                throw new ArgumentException("Unable to parse the IoT hub name from the connection string host name.", nameof(iotHubConnectionString));
            }

            // Establish the IoT hub connection via link to the necessary endpoint, which will trigger a redirect exception
            // from which the Event Hubs connection string can be built.

            var serviceEndpoint = new Uri($"{AmqpConstants.SchemeAmqps}://{parsedConnectionString.HostName}/messages/events");
            var connection = default(AmqpConnection);
            var link = default(AmqpLink);
            string eventHubsHost = default;
            string eventHubName = default;

            try
            {
                connection = await CreateAndOpenConnectionAsync(
                        serviceEndpoint,
                        iotHubName,
                        parsedConnectionString.SharedAccessKeyName,
                        parsedConnectionString.SharedAccessKey,
                        cancellationToken)
                    .ConfigureAwait(false);
                link = await CreateRedirectLinkAsync(connection, serviceEndpoint, cancellationToken).ConfigureAwait(false);

                await link.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (AmqpException ex) when (ex?.Error?.Condition.Value == AmqpErrorCode.LinkRedirect.Value && (ex?.Error?.Info != null))
            {
                // The Event Hubs host is returned as a first-party element of the redirect information.

                ex.Error.Info.TryGetValue("hostname", out eventHubsHost);

                // The Event Hub name is a variant of the IoT hub name and must be parsed from the
                // full IoT hub address returned by the redirect.

                if (ex.Error.Info.TryGetValue("address", out string iotAddress))
                {
                    //  If the address does not match the expected pattern, this will not result in an exception; the Event Hub
                    // name will remain null and trigger a failed validation later in the flow.

                    eventHubName = s_eventHubNameExpression.Match(iotAddress).Groups["eventHubName"].Value;
                }
            }
            finally
            {
                link?.Session?.SafeClose();
                link?.SafeClose();
                connection?.SafeClose();
            }

            // Attempt to assemble the Event Hubs connection string using the IoT hub components.

            if (string.IsNullOrEmpty(eventHubsHost))
            {
                throw new InvalidOperationException("The Event Hubs host was not returned by the IoT hub service.");
            }

            if (string.IsNullOrEmpty(eventHubName))
            {
                throw new InvalidOperationException("The Event Hub name was not returned by the IoT hub service.");
            }

            return $"Endpoint=sb://{eventHubsHost}/;EntityPath={eventHubName};SharedAccessKeyName={parsedConnectionString.SharedAccessKeyName};SharedAccessKey={parsedConnectionString.SharedAccessKey}";
        }

        /// <summary>
        /// Performs the tasks needed to build and open a connection to the IoT hub service.
        /// </summary>
        private static async Task<AmqpConnection> CreateAndOpenConnectionAsync(
            Uri serviceEndpoint,
            string iotHubName,
            string sharedAccessKeyName,
            string sharedAccessKey,
            CancellationToken cancellationToken)
        {
            string hostName = serviceEndpoint.Host;
            string userName = $"{sharedAccessKeyName}@sas.root.{iotHubName}";
            string signature = SharedAccessSignatureBuilder.BuildSignature(
                sharedAccessKeyName,
                sharedAccessKey,
                $"{hostName}{serviceEndpoint.AbsolutePath}",
                TimeSpan.FromMinutes(5));
            int port = 5671;

            // Create the layers of settings needed to establish the connection.

            var amqpVersion = new Version(1, 0, 0, 0);

            var tcpSettings = new TcpTransportSettings
            {
                Host = hostName,
                Port = port,
                ReceiveBufferSize = AmqpConstants.TransportBufferSize,
                SendBufferSize = AmqpConstants.TransportBufferSize,
            };

            var transportSettings = new TlsTransportSettings(tcpSettings)
            {
                TargetHost = hostName,
            };

            var connectionSettings = new AmqpConnectionSettings
            {
                IdleTimeOut = (uint)TimeSpan.FromMinutes(1).TotalMilliseconds,
                MaxFrameSize = AmqpConstants.DefaultMaxFrameSize,
                ContainerId = Guid.NewGuid().ToString(),
                HostName = hostName,
            };

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(new AmqpVersion(amqpVersion));
            saslProvider.AddHandler(new SaslPlainHandler { AuthenticationIdentity = userName, Password = signature });

            var amqpProvider = new AmqpTransportProvider();
            amqpProvider.Versions.Add(new AmqpVersion(amqpVersion));

            var amqpSettings = new AmqpSettings();
            amqpSettings.TransportProviders.Add(saslProvider);
            amqpSettings.TransportProviders.Add(amqpProvider);

            // Create and open the connection, respecting the timeout constraint
            // that was received.

            var initiator = new AmqpTransportInitiator(amqpSettings, transportSettings);
            TransportBase transport = await initiator.ConnectAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var connection = new AmqpConnection(transport, amqpSettings, connectionSettings);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                return connection;
            }
            catch
            {
                transport.Abort();
                throw;
            }
        }

        /// <summary>
        /// Creates the AMQP link used to trigger a redirection response from the IoT hub service.
        /// </summary>
        private static async Task<AmqpLink> CreateRedirectLinkAsync(
            AmqpConnection connection,
            Uri serviceEndpoint,
            CancellationToken cancellationToken)
        {
            string linkPath = $"{serviceEndpoint.AbsolutePath}/$management";
            var session = default(AmqpSession);

            try
            {
                var sessionSettings = new AmqpSessionSettings { Properties = new Fields() };
                session = connection.CreateSession(sessionSettings);

                await session.OpenAsync(cancellationToken).ConfigureAwait(false);

                var linkSettings = new AmqpLinkSettings
                {
                    Role = true,
                    TotalLinkCredit = 1,
                    AutoSendFlow = true,
                    SettleType = SettleMode.SettleOnSend,
                    Source = new Source { Address = linkPath },
                    Target = new Target { Address = serviceEndpoint.AbsoluteUri },
                };

                var link = new ReceivingAmqpLink(linkSettings);
                linkSettings.LinkName = $"{nameof(EventHubConnectionStringBuilder)};{connection.Identifier}:{session.Identifier}:{link.Identifier}";
                link.AttachTo(session);

                return link;
            }
            catch
            {
                session?.Abort();
                throw;
            }
        }
    }
}
