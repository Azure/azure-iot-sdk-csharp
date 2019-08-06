// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class Program
    {
        private static Dictionary<string, Tuple<string, Func<PerfScenarioConfig, PerfScenario>>> s_scenarios
            = new Dictionary<string, Tuple<string, Func<PerfScenarioConfig, PerfScenario>>>()
        {
            {"generate_iothub_config",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Generate the IoT Hub configuration required for the test (creates multiple devices).",
                    (c) => {return new GenerateIotHubConfigTest(c);})},

            { "device_all",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Devices connecting to IoT Hub then using multiple features.",
                    (c) => {return new DeviceAllTest(c);})},

            { "device_all_noretry",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Like device_all but will disable retries and create a new DeviceClient when the previous enters a faulted state.",
                    (c) => {return new DeviceAllNoRetry(c);})},

            {"single_device_d2c",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "A single device sending many events to IoT Hub.",
                    (c) => {return new DeviceOneD2CTest(c);}) },

            {"device_d2c",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Devices sending events to IoT Hub.",
                    (c) => {return new DeviceD2CTest(c);})},

            {"device_c2d",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Devices receiving events from the IoT Hub.",
                    (c) => {return new DeviceC2DTest(c);}) },

            {"device_method",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Devices receiving method calls from IoT Hub.",
                    (c) => {return new DeviceMethodTest(c);}) },

            { "device_d2c_noretry",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Like device_d2c but will disable retries and create a new DeviceClient when the previous enters a faulted state.",
                    (c) => {return new DeviceD2CNoRetry(c);})},

            { "device_c2d_noretry",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Like device_c2d but will disable retries and create a new DeviceClient when the previous enters a faulted state.",
                    (c) => {return new DeviceC2DNoRetry(c);})},

            { "device_methods_noretry",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Like device_methods but will disable retries and create a new DeviceClient when the previous enters a faulted state.",
                    (c) => {return new DeviceMethodsNoRetry(c);})},

            {"service_c2d",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "ServiceClient sending events to devices through IoT Hub.",
                    (c) => {return new ServiceC2DTest(c);}) },

            {"service_method",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "ServiceClient calling methods on devices through IoT Hub.",
                    (c) => {return new ServiceMethodTest(c);}) },

            { "baseline_noop",
                new Tuple<string, Func<PerfScenarioConfig, PerfScenario>>(
                    "Exercises the harness and metrics logging. No-op within any of the stages.",
                    (c) => {return new HarnessBaseline(c);})},
        };

        private static void Help()
        {
            Console.WriteLine(
                "Usage: \n\n" +
                "   dotnet run -- [-topslnaci] -f <scenario>\n" +
                "   iotclientperf [-topslnaci] -f <scenario>\n" +
                "       -t <seconds>    : Execution time (default 10 seconds).\n" +
                "       -o <path>       : Output path (default outputs to console).\n" +
                "       -p <protocol>   : Protocol (default mqtt). \n" +
                "                         Possible values: mqtt | mqtt_ws | amqp | amqp_ws | http.\n" +
                "       -s <bytes>      : Payload size (default 128 bytes). This depends on the scenario.\n" +
                "       -l <parallel_op>: Maximum parallel operations. (default 100 operations/scenarios in parallel).\n" +
                "       -n <count>      : Number of scenario instances. (default 1 instance).\n" + 
                "       -a <authType>   : Authentication type (default sas).\n" +
                "                         Possible values: sas | sas_policy | x509 \n" +
                "       -c <connections>: Enables AMQP Pooling. The connection pool size. (default -1: disabled)\n" +
                "                         This setting is ignored transport types other than AMQP.\n" +
                "       -i <runId>      : Test runID. (default: random ID).\n" +
                "       -f <scenario>   : Scenario name. One of the following: \n"
            );

            foreach (string scenario in s_scenarios.Keys)
            {
                Console.WriteLine($"       {scenario,-25}: {s_scenarios[scenario].Item1}");
            }
        }

        private static Dictionary<string, Client.TransportType> s_transportDictionary = new Dictionary<string, Client.TransportType>()
        {
            {"mqtt", Client.TransportType.Mqtt_Tcp_Only },
            {"mqtt_ws", Client.TransportType.Mqtt_WebSocket_Only },
            {"amqp", Client.TransportType.Amqp_Tcp_Only },
            {"amqp_ws", Client.TransportType.Amqp_WebSocket_Only},
            {"http", Client.TransportType.Http1 },
        };

        public static int Main(string[] args)
        {
            Console.WriteLine("IoT Client Performance test");

            if (args.Length < 1)
            {
                Help();
                return -1;
            }

            int param_counter = 0;
            int t = 10;
            string o = null;
            string p = "mqtt";
            int s = 128;
            int l = 100;
            int n = 1;
            string a = "sas";
            int c = -1;
            string i = null;
            string f = null;

            while (param_counter + 1 < args.Length)
            {
                switch (args[param_counter])
                {
                    case "--":
                        break;

                    case "-t":
                        t = int.Parse(args[++param_counter], CultureInfo.InvariantCulture);
                        break;

                    case "-o":
                        o = args[++param_counter];
                        break;

                    case "-p":
                        p = args[++param_counter];
                        break;

                    case "-s":
                        s = int.Parse(args[++param_counter], CultureInfo.InvariantCulture);
                        break;

                    case "-l":
                        l = int.Parse(args[++param_counter], CultureInfo.InvariantCulture);
                        break;

                    case "-n":
                        n = int.Parse(args[++param_counter], CultureInfo.InvariantCulture);
                        break;

                    case "-a":
                        a = args[++param_counter];
                        break;

                    case "-c":
                        c = int.Parse(args[++param_counter], CultureInfo.InvariantCulture);
                        break;

                    case "-i":
                        i = args[++param_counter];
                        break;

                    case "-f":
                        f = args[++param_counter];
                        break;

                    default:
                        Console.WriteLine($"Unknown parameter: {args[param_counter]}.");
                        return -1;
                }

                param_counter++;
            }

            if (f == null)
            {
                Console.Error.WriteLine("Missing -f <scenario> parameter.");
                Help();
                return -1;
            }

            if (i == null)
            {
                i = $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffZ")}_dotNet_{f}";
            }

            Tuple<string, Func<PerfScenarioConfig, PerfScenario>> scenario;
            Func<PerfScenarioConfig, PerfScenario> scenarioFactory;
            if (!s_scenarios.TryGetValue(f, out scenario))
            {
                Console.Error.WriteLine($"Unknown scenario: {f}");
                return -1;
            }

            scenarioFactory = scenario.Item2;

            Client.TransportType transportType;
            if (!s_transportDictionary.TryGetValue(p, out transportType))
            {
                Console.Error.WriteLine($"Unknown transport type: {p}");
                return -1;
            }

            ResultWriter resultWriter;
            if (o == null)
            {
                resultWriter = new ResultWriterConsole();
            }
            else
            {
                resultWriter = new ResultWriterFile(o, TelemetryMetrics.GetHeader());
            }

            Console.CancelKeyPress += delegate
            {
                // Make sure we flush the log before the process is terminated.
                Console.Write("Aborted. Writing output . . . ");
                resultWriter.FlushAsync().GetAwaiter().GetResult();
                Console.WriteLine("OK");
            };

            var runner = new PerfTestRunner(
                resultWriter,
                t,
                transportType,
                s,
                l,
                n,
                a,
                c,
                f,
                i,
                scenarioFactory);

            try
            {
                runner.RunTestAsync().GetAwaiter().GetResult();
            }
            finally
            {
                Console.Write("Writing output . . . ");
                resultWriter.FlushAsync().GetAwaiter().GetResult();
                Console.WriteLine("OK");
            }

            return 0;
        }
    }
}
