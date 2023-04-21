// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.LongHaul.AmqpPooling.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class Program
    {
        private static readonly Dictionary<string, string> s_commonProperties = new();

        private static Logger s_logger;
        private static int s_port;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;

        private static async Task Main(string[] args)
        {
            string deviceSdkVersionInfo = typeof(IotHubDeviceClient).Assembly
               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
               .InformationalVersion;

            string serviceSdkVersionInfo = typeof(IotHubServiceClient).Assembly
               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
               .InformationalVersion;

            s_commonProperties.Add(TestType, "AmqpPooling");
            s_commonProperties.Add(SdkLanguage, ".NET");
            s_commonProperties.Add(DeviceSdkVersion, deviceSdkVersionInfo);
            s_commonProperties.Add(ServiceSdkVersion, serviceSdkVersionInfo);

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

            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            s_logger = InitializeLogging(parameters);
            s_port = PortFilter(parameters);

            // Log system health before initializing DeviceManager
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(StartingRun);

            using var registerManager = new DeviceManager(s_logger, parameters);

            // Set up a condition to quit the sample
            Console.WriteLine("Press CTRL+C to exit");
            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("Exiting...");
            };

            // Register devices to the IoT hub
            IList<Device> devices = await registerManager.AddDevicesAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            // Log system health before initializing DeviceManager
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);
            await using var iotHub = new IotHub(s_logger, parameters, devices);

            foreach (KeyValuePair<string, string> prop in s_commonProperties)
            {
                iotHub.TelemetryUserProperties.Add(prop.Key, prop.Value);
            }

            await iotHub.InitializeAsync().ConfigureAwait(false);

            var systemHealthMonitor = new SystemHealthMonitor(iotHub, s_port, s_logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cancellationTokenSource.Token),
                        iotHub.SendTelemetryMessagesAsync(cancellationTokenSource.Token))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { } // user signaled an exit
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { } // user signaled an exit
            catch (Exception ex)
            {
                s_logger.Trace($"Long-haul testing with Amqp Pooling failed with exception {ex}", TraceSeverity.Error);
            }
            finally
            {
                // Clean up devices for Amqp pooling long-haul testing
                await registerManager.RemoveDevicesAsync().ConfigureAwait(false);
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
            var helper = new IotHubConnectionStringHelper(parameters.IotHubConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceTransportSettings, parameters.GetTransportSettingsWithPooling().ToString() },
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
            return parameters.DeviceTransportProtocol == IotHubClientTransportProtocol.WebSocket ? 443 : 5671;
        }
    }
}