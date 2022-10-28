using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Client.Samples;
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

            var sample = new ComputeDerivedKeySample(parameters);
            Console.WriteLine("DEBUG STATEMENT: RUNNING SAMPLE");
            sample.RunSample();

            return 0;
        }
    }
}
