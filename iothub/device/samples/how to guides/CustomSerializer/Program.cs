// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Google.Protobuf;
using Microsoft.Azure.Devices.Client;

namespace CustomSerializer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            PayloadConvention protoSerializer = new ProtobufPayloadConvention(Telemetries.Parser);
            string cs = "HostName=<hub>.azure-devices.net;DeviceId=<did>;SharedAccessKey=<key>";
            await using IotHubDeviceClient client = new IotHubDeviceClient(cs, new IotHubClientOptions() { PayloadConvention = protoSerializer });
            await client.OpenAsync();


            await client.UpdateReportedPropertiesAsync(new ReportedProperties
            {
                { "my Prop", 123 }
            });

            await client.SetDirectMethodCallbackAsync(async req =>
            {
                await Console.Out.WriteLineAsync(req.MethodName);
                if (req.TryGetPayload<Telemetries>(out Telemetries hoy))
                {
                    await Console.Out.WriteLineAsync(hoy.WorkingSet.ToString());
                }
                else
                {
                    await Console.Out.WriteLineAsync("cannot get payload");
                }

                return await Task.FromResult(new DirectMethodResponse(200) { Payload = "got the result"});
            });

            while (true)
            {
                var msg = new Telemetries() { WorkingSet = Environment.WorkingSet };
                await client.SendTelemetryAsync(new TelemetryMessage(msg));
                await Console.Out.WriteLineAsync("sending msg");
                await Task.Delay(2000);
            }

        }
    }
}