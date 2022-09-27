using System;
using System.Threading.Tasks;
using CommandLine;

namespace Microsoft.Azure.Devices.Samples
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            if (!parameters.Validate())
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            using var serviceClient = new IotHubServiceClient(parameters.HubConnectionString);

            var sample = new EdgeDeploymentSample(serviceClient);
            await sample.RunSampleAsync();

            Console.WriteLine("Done.");
        }
    }
}