using Mash.Logging;
using Mash.Logging.ApplicationInsights;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.IoT.Thief.Device;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.IoT.Thief.Device.LoggingConstants;

namespace ThiefDevice
{
    class Program
    {
        private static readonly IDictionary<string, string> _commonProperties = new Dictionary<string, string>();
        private static Settings _settings;
        private static Logger _logger;
        private static IotHub _iotHub;

        static async Task Main(string[] args)
        {
            _commonProperties.Add(RunId, Guid.NewGuid().ToString());
            _commonProperties.Add(SdkLanguage, ".NET");
            _commonProperties.Add(SdkVersion, "1.34.0");

            _settings = InitializeSettings();
            _logger = InitializeLogging(_settings.DeviceConnectionString, _settings.AiKey, _settings.TransportProtocolType);
            _iotHub = InitializeHub(_logger);

            _logger.Event(StartingRun);

            await _iotHub.InitializeAsync().ConfigureAwait(false);
            using CancellationTokenSource cancellationTokenSource = ConfigureAppExit();
            var systemHealthMonitor = new SystemHealthMonitor(_iotHub, _logger.Clone());

            try
            {
                await Task
                    .WhenAll(
                        systemHealthMonitor.RunAsync(cancellationTokenSource.Token),
                        _iotHub.RunAsync(cancellationTokenSource.Token))
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException) { } // user signalled an exit

            _iotHub.Dispose();
            _logger.Flush();
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

        private static Settings InitializeSettings()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string workingDirectory = Path.GetDirectoryName(path);
            string commonAppSettings = Path.Combine(workingDirectory, "Settings", "common.config.json");
            string userAppSettings = Path.Combine(workingDirectory, "Settings", $"{Environment.UserName}.config.json");

            return new ConfigurationBuilder()
                .AddJsonFile(commonAppSettings)
                .AddJsonFile(userAppSettings, true)
                .Build()
                .Get<Settings>();
        }

        private static Logger InitializeLogging(string deviceConnectionString, string aiKey, string transportProtocolType)
        {
            var helper = new IotHubConnectionStringHelper();
            helper.GetValuesFromConnectionString(deviceConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceId, helper.DeviceId },
                    { Transport, transportProtocolType },
                },
            };
            foreach (var kvp in _commonProperties)
            {
                logBuilder.AppContext.Add(kvp.Key, kvp.Value);
            }
            logBuilder.LogProviders.Add(new ConsoleLogProvider { ShouldLogContext = false, ShouldUseColor = true });
            logBuilder.LogProviders.Add(new ApplicationInsightsLoggingProvider(aiKey));

            var logger = logBuilder.BuildLogger();
            return logger;
        }

        private static IotHub InitializeHub(Logger logger)
        {
            var iotHub = new IotHub(logger, _settings.DeviceConnectionString, GetTransportSettings(_settings.TransportProtocolType));
            foreach (var prop in _commonProperties)
            {
                iotHub.IotProperties.Add(prop.Key, prop.Value);
            }

            return iotHub;
        }

        private static IotHubClientTransportSettings GetTransportSettings(string transportProtocolType)
        {
            switch (transportProtocolType)
            {
                case "Amqp":
                case "Amqp_Tcp_only":
                    return new IotHubClientAmqpSettings();

                case "Amqp_Websocket_Only":
                    return new IotHubClientAmqpSettings(IotHubClientTransportProtocol.WebSocket);

                case "Mqtt":
                case "Mqtt_Tcp_only":
                    return new IotHubClientMqttSettings();

                case "Mqtt_Websocket_Only":
                    return new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket);

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
