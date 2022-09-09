// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// NOTE: DO NOT USE THIS IN TESTS
    /// This class logs to multiple providers - Event source, Application insights.
    /// This is the global singleton logger Instance.
    /// </summary>
    public class TestLogger
    {
        public static readonly TestLogger Instance = new();

        public const string SdkLanguage = ".NET";
        public const string Service = "IotHub";

        // Client to log to application insights.
        private readonly TelemetryClient _telemetryClient;

        private readonly IDictionary<string, string> _commonProperties;

        private TestLogger()
        {
            // Instrumentation key is used to connect to Application Insights instance.
            // The value is copied from the portal and stored in KeyVault.
            // The E2E tests script will load it as an environment variable and the pipelines have a task to do the same.
            string intrumentationKey = Environment.GetEnvironmentVariable("E2E_IKEY");
            if (!string.IsNullOrWhiteSpace(intrumentationKey))
            {
                using var config = new TelemetryConfiguration
                {
                    InstrumentationKey = intrumentationKey,
                };
                _telemetryClient = new TelemetryClient(config);

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
                    { LoggingPropertyNames.Attempt, Environment.GetEnvironmentVariable("SYSTEM_JOBATTEMPT") },
                    { LoggingPropertyNames.TargetBranch, Environment.GetEnvironmentVariable("TARGET_BRANCH") },
                };
            }
        }

        private const string NullInstance = "(null)";

        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        public void Trace(string message, SeverityLevel severity = SeverityLevel.Information, IDictionary<string, string> extraProperties = null)
        {
            // Log to event source
            EventSourceTestLogger.Log.TestMessage(message);

            // Log to Application insights
            if (_telemetryClient != null)
            {
                IDictionary<string, string>[] bagsToMerge = new[] { _commonProperties, extraProperties };
                _telemetryClient.TrackTrace(message, severity, MergePropertyBags(bagsToMerge));
            }
        }

        /// <summary>
        /// Flush to ensure logs are not lost before quiting an application.
        /// </summary>
        public async Task SafeFlushAsync()
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.Flush();

                // Hold on to the thread context to allow flushing to complete.
                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        public static IDictionary<string, string> MergePropertyBags(IDictionary<string, string>[] propertyBags)
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
    }
}
