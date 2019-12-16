// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Azure.Devices.DigitalTwin.Client
{
    [EventSource(Name = "Microsoft-Azure-IoT-DigitalTwin-Device")]
    internal sealed class Logging : EventSource, ILogging
    {
        /// <summary>The single event source instance to use for all logging.</summary>
        private static ILogging log;

        // Common event reservations: [1, 10)
        private const int CriticalEventId = 1;
        private const int WarningEventId = 2;
        private const int ErrorEventId = 3;
        private const int InformationalEventId = 4;
        private const int VerboseEventId = 5;

        // The Instance constructor is private, to enforce singleton semantics.
        private Logging()
            : base()
        {
        }

        public static ILogging Instance
        {
            get
            {
                if (log == null)
                {
                    log = new Logging();
                }

                return log;
            }

            internal set
            {
                log = value;
            }
        }

        [Event(CriticalEventId, Level = EventLevel.Critical)]
        private void LogCritical(string message, string source)
        {
            this.WriteEvent(CriticalEventId, $"{message}{Environment.NewLine}{source}");
        }

        [NonEvent]
        public void LogCritical(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            this.LogCritical(message, this.GetSource(memberName, sourceFilePath, sourceLineNumber));
        }

        [Event(WarningEventId, Level = EventLevel.Warning)]
        private void LogWarning(string message, string source)
        {
            this.WriteEvent(WarningEventId, $"{message}{Environment.NewLine}{source}");
        }

        [NonEvent]
        public void LogWarning(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            this.LogWarning(message, this.GetSource(memberName, sourceFilePath, sourceLineNumber));
        }


        [Event(ErrorEventId, Level = EventLevel.Error)]
        private void LogError(string message, string source)
        {
            this.WriteEvent(ErrorEventId, $"{message}{Environment.NewLine}{source}");
        }

        [NonEvent]
        public void LogError(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            this.LogError(message, this.GetSource(memberName, sourceFilePath, sourceLineNumber));
        }

        [Event(InformationalEventId, Level = EventLevel.Informational)]
        private void LogInformational(string message, string source)
        {
            this.WriteEvent(InformationalEventId, $"{message}{Environment.NewLine}{source}");
        }

        [NonEvent]
        public void LogInformational(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            this.LogInformational(message, this.GetSource(memberName, sourceFilePath, sourceLineNumber));
        }

        [Event(VerboseEventId, Level = EventLevel.Verbose)]
        private void LogVerbose(string message, string source)
        {
            this.WriteEvent(VerboseEventId, $"{message}{Environment.NewLine}{source}");
        }

        [NonEvent]
        public void LogVerbose(string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            this.LogVerbose(message, this.GetSource(memberName, sourceFilePath, sourceLineNumber));
        }

        [NonEvent]
        private string GetSource(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            return $"Method '{memberName}' in {sourceFilePath} line {sourceLineNumber}";
        }
    }
}
