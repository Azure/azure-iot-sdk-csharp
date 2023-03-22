using CommandLine;
using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoT.Thief.Device;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.IoT.Thief.Device.LoggingConstants;

namespace ThiefDevice
{
    internal class Program
    {
        private static readonly IDictionary<string, string> s_commonProperties = new Dictionary<string, string>();
        private static readonly string s_deviceConnectionString = Environment.GetEnvironmentVariable("IOTHUB_LONG_HAUL_DEVICE_CONNECTION_STRING");
        private static readonly string s_instrumentationKey = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_INSTRUMENTATION_KEY");
        private static Logger s_logger;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;
        private static Parameters s_parameters;

        private static async Task Main(string[] args)
        {
            s_commonProperties.Add(RunId, Guid.NewGuid().ToString());
            s_commonProperties.Add(SdkLanguage, ".NET");
            // TODO: get this info at runtime rather than hard-coding it
            s_commonProperties.Add(SdkVersion, "2.0.0-preview004");

            // Parse application parameters
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    s_parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            s_logger = InitializeLogging(s_parameters.ConnectionString ?? s_deviceConnectionString);

            // Log system health before initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(StartingRun);

            await using var iotHub = new IotHub(
                s_logger,
                s_parameters.ConnectionString ?? s_deviceConnectionString,
                GetTransportSettings());
            foreach (KeyValuePair<string, string> prop in s_commonProperties)
            {
                iotHub.IotProperties.Add(prop.Key, prop.Value);
            }

            // Log system health after initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);
            await iotHub.InitializeAsync().ConfigureAwait(false);

            // Log system health after opening connection to hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            using CancellationTokenSource cancellationTokenSource = ConfigureAppExit();
            var systemHealthMonitor = new SystemHealthMonitor(iotHub, s_logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cancellationTokenSource.Token),
                        iotHub.RunAsync(cancellationTokenSource.Token))
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) { } // user signalled an exit
            catch (Exception ex)
            {
                s_logger.Trace($"Device app failed with exception {ex}");
            }

            await iotHub.DisposeAsync().ConfigureAwait(false);

            // Log system health after disposing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Flush();
            s_aiLoggingProvider.Dispose();
        }

        private static CancellationTokenSource ConfigureAppExit()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("Exiting ...");
            };
            Console.WriteLine("Press CTRL+C to exit");
            return cancellationTokenSource;
        }

        private static Logger InitializeLogging(string deviceConnectionString)
        {
            var helper = new IotHubConnectionStringHelper(deviceConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceId, helper.DeviceId },
                    { Transport, GetTransportSettings().ToString() },
                },
            };
            foreach (KeyValuePair<string, string> kvp in s_commonProperties)
            {
                logBuilder.AppContext.Add(kvp.Key, kvp.Value);
            }
            logBuilder.LogProviders.Add(new ConsoleLogProvider { ShouldLogContext = false, ShouldUseColor = true });
            s_aiLoggingProvider = new ApplicationInsightsLoggingProvider(s_parameters.InstrumentationKey ?? s_instrumentationKey);
            logBuilder.LogProviders.Add(s_aiLoggingProvider);

            Logger logger = logBuilder.BuildLogger();
            return logger;
        }

        private static IotHubClientTransportSettings GetTransportSettings()
        {
            return s_parameters.Transport switch
            {
                TransportType.Mqtt => new IotHubClientMqttSettings(s_parameters.TransportProtocol),
                TransportType.Amqp => new IotHubClientAmqpSettings(s_parameters.TransportProtocol),
                _ => throw new NotSupportedException($"Unsupported transport type {s_parameters.Transport}/{s_parameters.TransportProtocol}"),
            };
        }
    }
}
