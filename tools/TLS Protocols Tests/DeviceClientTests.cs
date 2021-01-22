using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TlsProtocolTests
{
    internal static class DeviceClientTests
    {
        public static async Task RunTest(string deviceCs)
        {
            Console.WriteLine("Starting device client tests.");

            int i = 0;
            int successes = 0;
            int failures = 0;
            const string messageFormat = "{{ \"deviceClientTestNumber\": {0}, \"transportType\": \"{1}\" }}";

            foreach (TransportType transportType in Enum.GetValues(typeof(TransportType)))
            {
                using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceCs, transportType))
                {
                    try
                    {
                        string messageBody = string.Format(messageFormat, ++i, transportType.ToString());

                        Console.WriteLine($"Sending: {messageBody}");

                        // If no time out is specified, the API will attempt to run indefinitely
                        using (var cts = new CancellationTokenSource(5000))
                        {
                            await deviceClient
                                .SendEventAsync(
                                    new Message(Encoding.UTF8.GetBytes(messageBody))
                                    {
                                        ContentType = "application/json",
                                        ContentEncoding = "UTF-8",
                                    },
                                    cts.Token)
                                .ConfigureAwait(false);
                        }

                        Console.WriteLine("Succeeded.\n");
                        successes++;
                    }
                    catch (Exception ex)
                    {
                        // Print all the relevant reasons for failing, without printing out the entire exception information
                        var reason = new StringBuilder();

                        Exception next = ex;
                        do
                        {
                            reason.AppendFormat($" - {next.GetType()}: {next.Message}\n");
                            next = next.InnerException;
                        }
                        while (next != null);
                        Console.WriteLine($"Failed for {transportType} due to:\n{reason}");
                        failures++;
                    }
                }
            }

            Console.WriteLine($"DeviceClient tests finished with {successes} successes and {failures} failures.");
        }
    }
}
