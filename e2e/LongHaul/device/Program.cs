using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using static Microsoft.Azure.Devices.LongHaul.Device.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal class Program
    {
        private static readonly IDictionary<string, string> s_commonProperties = new Dictionary<string, string>();
        private static Logger s_logger;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;
        internal static readonly string _runId = Guid.NewGuid().ToString();

        private static async Task Main(string[] args)
        {
            string sdkVersionInfo = typeof(IotHubDeviceClient).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            s_commonProperties.Add(TestClient, "IotHubDeviceClient");
            s_commonProperties.Add(RunId, _runId);
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

            // Log system health before initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(StartingRun);

            await using var iotHub = new IotHub(
                s_logger,
                parameters.ConnectionString,
                GetTransportSettings(parameters));
            foreach (KeyValuePair<string, string> prop in s_commonProperties)
            {
                iotHub.TelemetryUserProperties.Add(prop.Key, prop.Value);
            }

            // Log system health after initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);
            await iotHub.InitializeAsync().ConfigureAwait(false);

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

            await iotHub.SetPropertiesAsync(LoggingConstants.RunId, _runId, cancellationTokenSource.Token).ConfigureAwait(false);

            var systemHealthMonitor = new SystemHealthMonitor(iotHub, s_logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cancellationTokenSource.Token),
                        iotHub.SendTelemetryMessagesAsync(cancellationTokenSource.Token),
                        iotHub.ReportReadOnlyPropertiesAsync(cancellationTokenSource.Token),
                        iotHub.UploadFilesAsync(cancellationTokenSource.Token))
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) { } // user signalled an exit
            catch (Exception ex)
            {
                s_logger.Trace($"Device app failed with exception {ex}", TraceSeverity.Error);
            }

            await iotHub.DisposeAsync().ConfigureAwait(false);

            // Log system health after disposing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Flush();
            s_aiLoggingProvider.Dispose();
        }

        private static Logger InitializeLogging(Parameters parameters)
        {
            var helper = new IotHubConnectionStringHelper(parameters.ConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceId, helper.DeviceId },
                    { Transport, GetTransportSettings(parameters).ToString() },
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

        private static IotHubClientTransportSettings GetTransportSettings(Parameters parameters)
        {
            return parameters.Transport switch
            {
                TransportType.Mqtt => new IotHubClientMqttSettings(parameters.TransportProtocol),
                TransportType.Amqp => new IotHubClientAmqpSettings(parameters.TransportProtocol),
                _ => throw new NotSupportedException($"Unsupported transport type {parameters.Transport}/{parameters.TransportProtocol}"),
            };
        }
    }
}
