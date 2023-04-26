using CommandLine;
using Microsoft.Azure.Devices.Logging;
using Microsoft.Azure.Devices.Provisioning.Client.Samples;
using Microsoft.Extensions.Logging;
using System;

namespace ComputeDerivedSymmetricKeySample
{
    internal class Program
    {
        private static int Main(string[] args)
        {
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

            // Set up logging
            using ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddColorConsoleLogger(
                new ColorConsoleLoggerConfiguration
                {
                    // The SDK logs are written at Trace level. Set this to LogLevel.Trace to get ALL logs.
                    MinLogLevel = LogLevel.Debug,
                });
            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            var sample = new ComputeDerivedKeySample(parameters, logger);
            sample.RunSample();

            return 0;
        }
    }
}
