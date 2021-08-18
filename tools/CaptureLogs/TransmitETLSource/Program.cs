using CommandLine;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TransmitETL
{
    class Program
    {
        public static long eventsProcessed = 0L;
        public static TelemetryClient tc = null;
        public static bool hasParameterErrors;


        static void Main(string[] args)
        {
            // Attach this exit event so we can log when the application stops
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                    Log($"Using session: {parameters.SessionName}");
                    Log($"Using Application Insights connection string: {parameters.ConnectionString}");
                    Log($"Using heartbeat interval: {parameters.HeartBeatInterval}s");
                })
                .WithNotParsed(errors =>
                {
                    hasParameterErrors = true;
                    Environment.Exit(1);
                });


            // Create the telemetry client using the specified connection string from the command line
            try
            {
                tc = new TelemetryClient(new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
                {
                    ConnectionString = parameters.ConnectionString
                });
            }
            catch (Exception ex)
            {
                Log("Error creating the Application Insights instance. See exception for more details.", false);
                Log(ex.Message, false);
                Environment.Exit(1);
            }

            // Create a heartbeat event that lets us know the application is running
            tc.TrackEvent("ApplicationStart");
            Task.Run(async () =>
            {
                while (true)
                {
                    tc.TrackEvent("Heartbeat", metrics: new Dictionary<string, double> { ["eventsProcessed"] = eventsProcessed });
                    Log($"Heartbeat, sent {eventsProcessed} events", false);
                    await Task.Delay(TimeSpan.FromSeconds(parameters.HeartBeatInterval));
                }
            });

            // Try to create the diagnostic listener that attaches to the specified session.
            try
            {
                Log($"Creating session listener.");
                using (var source = new ETWTraceEventSource(parameters.SessionName, TraceEventSourceType.Session))
                {
                    Log($"Creating session parser.");
                    var parser = new DynamicTraceEventParser(source);
                    parser.All += (TraceEvent data) =>
                    {
                        // Increment the events count for the heartbeat 
                        Interlocked.Increment(ref eventsProcessed);
                        var evtTelemetry = new EventTelemetry();
                        evtTelemetry.Timestamp = data?.TimeStamp != null ? data.TimeStamp : DateTime.Now ;
                        evtTelemetry.Name = $"{ReturnDefaultString(data?.ProviderName)}/{ReturnDefaultString(data?.EventName)}";
                        foreach (var item in data?.PayloadNames)
                        {
                            evtTelemetry.Properties.Add(item, ReturnDefaultString(data.PayloadStringByName(item)));
                        }
                        tc.TrackEvent(evtTelemetry);
                    };
                    Log($"Starting session processing.");
                    source.Process();
                }
            }
            catch (Exception ex)
            {
                Log("Error while processing event. See exception for more details.");
                Log(ex.Message, false);
                tc.TrackException(ex);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Simple null check to avoid exceptions.
        /// </summary>
        /// <param name="stringToCheck"></param>
        /// <returns></returns>
        private static string ReturnDefaultString(string stringToCheck)
        {
            return stringToCheck ?? string.Empty;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // Log that the application has exited. This usally means it was done gracefylly
            if (!hasParameterErrors)
            {
                Log($"Process is exiting with code {Environment.ExitCode}.");
            }
        }

        /// <summary>
        /// Log the event and optionally send it to Application Insights.
        /// </summary>
        /// <param name="Message">The string of the message to log.</param>
        /// <param name="useAppInsights"><c>fasle</c> to not send this message to Application Insights</param>
        private static void Log(string Message, bool useAppInsights = true)
        {
            System.Diagnostics.Trace.WriteLine(Message);
            if (useAppInsights)
            {
                tc?.TrackEvent(Message);
            }
        }
    }
}
