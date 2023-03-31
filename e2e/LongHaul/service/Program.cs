// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class Program
    {
        private static readonly IDictionary<string, string> s_commonProperties = new Dictionary<string, string>();
        private static Logger s_logger;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;

        private static async Task Main(string[] args)
        {
            s_commonProperties.Add(TestClient, "IotHubServiceClient");
            s_commonProperties.Add(RunId, Guid.NewGuid().ToString());
            s_commonProperties.Add(SdkLanguage, ".NET");
            // TODO: get this info at runtime rather than hard-coding it
            s_commonProperties.Add(SdkVersion, "2.0.0-preview004");

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

            // Log system health before initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(StartingRun);

            using var iotHub = new IotHub(s_logger, parameters);

            // Log system health after initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);
            iotHub.Initialize();

            // Log system health after opening connection to hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            // Set up a condition to quit the sample
            Console.WriteLine("Press CTRL+C to exit");
            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("Exiting ...");
            };

            var systemHealthMonitor = new SystemHealthMonitor(s_logger.Clone());
            var hubEvents = new HubEvents(iotHub, s_logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cancellationTokenSource.Token),
                        iotHub.MonitorConnectedDevicesAsync(cancellationTokenSource.Token),
                        iotHub.ReceiveMessageFeedbacksAsync(cancellationTokenSource.Token),
                        hubEvents.RunAsync(cancellationTokenSource.Token))
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { } // user signaled an exit
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { } // user signaled an exit
            catch (Exception ex)
            {
                s_logger.Trace($"Service app failed with exception {ex}", TraceSeverity.Error);
            }

            // Log system health after disposing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

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
                    { Transport, $"HTTP/{parameters.TransportProtocol}" },
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
    }
}
