// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using CommandLine;
using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.LongHaul.Module.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Module
{
    internal class Program
    {
        private static readonly Dictionary<string, string> s_commonProperties = new();
        private static Logger s_logger;
        private static int s_port;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;
        internal static readonly string s_runId = Guid.NewGuid().ToString();
        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(1);

        private static async Task Main(string[] args)
        {
            string sdkVersionInfo = typeof(IotHubDeviceClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            s_commonProperties.Add(TestClient, nameof(IotHubModuleClient));
            s_commonProperties.Add(RunId, s_runId);
            s_commonProperties.Add(SdkLanguage, ".NET");
            s_commonProperties.Add(SdkVersion, sdkVersionInfo);

            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            s_logger = InitializeLogging(parameters);
            s_port = PortFilter(parameters);

            s_logger.Event(StartingRun);

            await using var iotHub = new IotHub(s_logger, parameters);
            foreach (KeyValuePair<string, string> prop in s_commonProperties)
            {
                iotHub.TelemetryUserProperties.Add(prop.Key, prop.Value);
            }

            s_logger.Trace(
                $"The transport protocol [{parameters.Transport}/{parameters.TransportProtocol}] is applied into the device app.",
                TraceSeverity.Verbose);

            // Set up a condition to quit the sample
            Console.WriteLine("Press CTRL+C to exit");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Exiting...");
            };

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await iotHub.InitializeAsync(cts.Token).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    s_logger.Trace($"Exception initializing client\n{ex}", TraceSeverity.Warning);
                    await Task.Delay(s_retryInterval, cts.Token).ConfigureAwait(false);
                }
            }

            // Log system health after opening connection to hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            await iotHub.SetPropertiesAsync(RunId, s_runId, s_logger.Clone(), cts.Token).ConfigureAwait(false);

            var systemHealthMonitor = new SystemHealthMonitor(iotHub, s_port, s_logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cts.Token),
                        iotHub.SendMessagesToRouteAsync(s_logger.Clone(), cts.Token),
                        iotHub.ReportReadOnlyPropertiesAsync(s_logger.Clone(), cts.Token))
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) { } // user signalled an exit
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { } // user signaled an exit
            catch (Exception ex)
            {
                s_logger.Trace($"Device app failed with exception {ex}", TraceSeverity.Error);
            }

            await iotHub.DisposeAsync().ConfigureAwait(false);

            // Log system health after disposing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(EndingRun);
            s_logger.Flush();
            s_aiLoggingProvider.Dispose();
        }

        private static Logger InitializeLogging(Parameters parameters)
        {
            var helper = new IotHubConnectionStringHelper(parameters.GatewayHostName == null ? parameters.DeviceModuleConnectionString : parameters.EdgeModuleConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceId, helper.DeviceId },
                    { ModuleId, helper.ModuleId },
                    { Transport, parameters.GetTransportSettings().ToString() },
                },
            };
            foreach (KeyValuePair<string, string> kvp in s_commonProperties)
            {
                logBuilder.AppContext.Add(kvp.Key, kvp.Value);
            }
            logBuilder.LogProviders.Add(new ConsoleLogProvider { ShouldLogContext = false, ShouldUseColor = true });
            s_aiLoggingProvider = new ApplicationInsightsLoggingProvider(parameters.InstrumentationKey);
            logBuilder.LogProviders.Add(s_aiLoggingProvider);

            Logger logger = logBuilder.BuildLogger();
            return logger;
        }

        private static int PortFilter(Parameters parameters)
        {
            return parameters.TransportProtocol == IotHubClientTransportProtocol.WebSocket
                ? 443
                : parameters.Transport switch
                {
                    TransportType.Mqtt => 8883,
                    TransportType.Amqp => 5671,
                    _ => throw new NotSupportedException($"Unsupported transport type {parameters.Transport}/{parameters.TransportProtocol}"),
                };
        }
    }
}
