// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class logs to multiple providers - Event source, Application insights
    /// </summary>
    public class TestLogger
    {
        public const string SdkLanguage = ".NET";
        public const string Service = "IotHub";
        private static object _initLock = new object();

        // Client to log to application insights.
        private static TelemetryClient _telemetryClient;

        private static IDictionary<string, string> _commonProperties;

        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        protected TestLogger()
        {
            // Thread-safe initialization of the telemetry client and run Id.
            lock (_initLock)
            {
                if (_telemetryClient == null)
                {
                    // Instrumentation key is used to connect to Application Insights instance.
                    // The value is copied from the portal and stored in KeyVault.
                    // The E2E tests script will load it as an environment variable and the pipelines have a task to do the same.
                    string intrumentationKey = Environment.GetEnvironmentVariable("E2E_IKEY");
                    if (!string.IsNullOrWhiteSpace(intrumentationKey))
                    {
                        var config = new TelemetryConfiguration
                        {
                            InstrumentationKey = intrumentationKey,
                        };
                        _telemetryClient = new TelemetryClient(config);

                        InitializeCommonProperties();
                    }
                }
            }
        }

        private const string NullInstance = "(null)";

        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        public static TestLogger GetInstance()
        {
            return new TestLogger();
        }

        public void Trace(string message, SeverityLevel severity = SeverityLevel.Information, IDictionary<string, string> extraProperties = null)
        {
            // Log to event source
            EventSourceTestLogger.Log.TestMessage(message);

            // Log to Application insights
            if (_telemetryClient != null)
            {
                IDictionary<string, string>[] bagsToMerge = new[] { _commonProperties, Properties, extraProperties };
                _telemetryClient.TrackTrace(message, severity, MergePropertyBags(bagsToMerge));
            }
        }

        public void Event(string eventName, IDictionary<string, string> extraProperties = null)
        {
            // Log event to Application insights
            if (_telemetryClient != null)
            {
                IDictionary<string, string>[] bagsToMerge = new[] { _commonProperties, Properties, extraProperties };
                _telemetryClient.TrackEvent(eventName, MergePropertyBags(bagsToMerge));
            }
        }

        /// <summary>
        /// Flush to ensure logs are not lost before quiting an application.
        /// </summary>
        public void SafeFlush()
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.Flush();

                // Hold on to the thread context to allow flushing to complete.
                Thread.Sleep(5000);
            }
        }

        private IDictionary<string, string> MergePropertyBags(IDictionary<string, string>[] propertyBags)
        {
            var result = new Dictionary<string, string>();

            foreach (IDictionary<string, string> bag in propertyBags)
            {
                if (bag == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, string> property in bag)
                {
                    result.Add(property.Key, property.Value);
                }
            }

            return result;
        }

        private void InitializeCommonProperties()
        {
            _commonProperties = new Dictionary<string, string>
            {
                // The SDK language.
                { LoggingPropertyNames.SdkLanguage, SdkLanguage },
                // The Service for which we are logging.
                { LoggingPropertyNames.Service, Service },
                // Unique ID for all tests of a run.
                { LoggingPropertyNames.TestRunId, Guid.NewGuid().ToString() },
                // Build Id in the pipeline.
                { LoggingPropertyNames.BuildId, Environment.GetEnvironmentVariable("BUILD_BUILDID") },
            };
        }
    }
}
