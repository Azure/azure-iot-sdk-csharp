﻿using Mash.Logging;
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
    internal class Program
    {
        private static readonly IDictionary<string, string> s_commonProperties = new Dictionary<string, string>();
        private static Settings s_settings;
        private static Logger s_logger;
        private static ApplicationInsightsLoggingProvider s_aiLoggingProvider;

        private static async Task Main()
        {
            s_commonProperties.Add(RunId, Guid.NewGuid().ToString());
            s_commonProperties.Add(SdkLanguage, ".NET");
            // TODO: get this info at runtime rather than hard-coding it
            s_commonProperties.Add(SdkVersion, "2.0.0-preview004");

            s_settings = InitializeSettings();
            s_logger = InitializeLogging(
                s_settings.DeviceConnectionString,
                s_settings.AiKey,
                s_settings.TransportType,
                s_settings.TransportProtocol);

            // Log system health before initializing hub
            SystemHealthMonitor.BuildAndLogSystemHealth(s_logger);

            s_logger.Event(StartingRun);

            await using var iotHub = new IotHub(
                s_logger,
                s_settings.DeviceConnectionString,
                GetTransportSettings(s_settings.TransportType, s_settings.TransportProtocol));
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

        private static Settings InitializeSettings()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string codeBase = Path.GetDirectoryName(assembly);
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string commonAppSettings = Path.Combine(path, "Settings", "common.config.json");
            string userAppSettings = Path.Combine(path, "Settings", $"{Environment.UserName}.config.json");

            return new ConfigurationBuilder()
                .AddJsonFile(commonAppSettings)
                .AddJsonFile(userAppSettings, true)
                .Build()
                .Get<Settings>();
        }

        private static Logger InitializeLogging(
            string deviceConnectionString,
            string aiKey,
            TransportType transportType,
            IotHubClientTransportProtocol transportProtocol)

        {
            var helper = new IotHubConnectionStringHelper(deviceConnectionString);
            var logBuilder = new LoggingBuilder
            {
                AppContext =
                {
                    { Hub, helper.HostName },
                    { DeviceId, helper.DeviceId },
                    { Transport, GetTransportSettings(transportType, transportProtocol).ToString() },
                },
            };
            foreach (KeyValuePair<string, string> kvp in s_commonProperties)
            {
                logBuilder.AppContext.Add(kvp.Key, kvp.Value);
            }
            logBuilder.LogProviders.Add(new ConsoleLogProvider { ShouldLogContext = false, ShouldUseColor = true });
            s_aiLoggingProvider = new ApplicationInsightsLoggingProvider(aiKey);
            logBuilder.LogProviders.Add(s_aiLoggingProvider);

            Logger logger = logBuilder.BuildLogger();
            return logger;
        }

        private static IotHubClientTransportSettings GetTransportSettings(
            TransportType transportType,
            IotHubClientTransportProtocol transportProtocol)
        {
            return transportType switch
            {
                TransportType.Mqtt => new IotHubClientMqttSettings(transportProtocol),
                TransportType.Amqp => new IotHubClientAmqpSettings(transportProtocol),
                _ => throw new NotSupportedException($"Unsupported transport type {transportType}/{transportProtocol}"),
            };
        }
    }

    public enum TransportType
    {
        Mqtt,
        Amqp,
    };
}
