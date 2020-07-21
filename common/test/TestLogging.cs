// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class logs to multiple providers - Event source, Application insights
    /// </summary>
    public class TestLogging
    {
        public const string Language = "C#";
        public const string Service = "IotHub";
        private static object _initLock = new object();

        // Client to log to application insights
        private static TelemetryClient _telemetryClient;

        // Unique ID for all tests of a run.
        private static string _testRunId;

        public TestContext Context { get; set; }
        public string TargetFramework { get; set; }

        private TestLogging()
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
                        _testRunId = Guid.NewGuid().ToString();
                    }
                }
            }
        }

        private const string NullInstance = "(null)";

        public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

        public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

        public static TestLogging GetInstance(TestContext testContext = null)
        {
            var logger = new TestLogging();

            if (_telemetryClient != null)
            {
                // Set logger properties for the current test.
                var targetFramework = (TargetFrameworkAttribute)Assembly
                    .GetExecutingAssembly()
                    .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                    .SingleOrDefault();
                logger.Context = testContext;
                logger.TargetFramework = targetFramework.FrameworkName;
            };

            return logger;
        }

        public void WriteLine(string message, SeverityLevel severity = SeverityLevel.Information, IDictionary<string, string> propertyBag = null, [CallerMemberName] string caller = null)
        {
            // Log to event source
            EventSourceTestLogging.Log.TestMessage(message);

            // Log to Application insights
            if (_telemetryClient != null)
            {
                if (propertyBag == null)
                {
                    propertyBag = new Dictionary<string, string>();
                }

                // Add common properties
                propertyBag.Add(LoggingPropertyNames.TestRunId, _testRunId);
                propertyBag.Add(LoggingPropertyNames.TestFramework, TargetFramework);
                propertyBag.Add(LoggingPropertyNames.Language, Language);
                propertyBag.Add(LoggingPropertyNames.Service, Service);
                if (Context != null)
                {
                    propertyBag.Add(LoggingPropertyNames.TestName, Context.TestName);
                    propertyBag.Add(LoggingPropertyNames.ClassName, Context.FullyQualifiedTestClassName ?? caller);
                    propertyBag.Add(LoggingPropertyNames.TestStatus, Context?.CurrentTestOutcome.ToString());
                }

                // Environment Variable set in Azure pipelines to correlate logs with the build number.
                propertyBag.Add("BuildId", Environment.GetEnvironmentVariable("BUILD_BUILDID"));

                _telemetryClient.TrackTrace(message, severity, propertyBag);
            }
        }

        public void Flush()
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.Flush();
            }
        }
    }
}
