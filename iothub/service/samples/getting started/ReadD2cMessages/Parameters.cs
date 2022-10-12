// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.Azure.Devices.Samples.ReadD2CMessages
{
    /// <summary>
    /// Parameters for the application.
    /// </summary>
    internal class Parameters
    {
        [Option(
            'e',
            "EventHubCompatibleEndpoint",
            HelpText = "The event hub-compatible endpoint from your IoT hub instance. Use `az iot hub show --query properties.eventHubEndpoints.events.endpoint --name {your IoT hub name}` to fetch via the Azure CLI.")]
        public string EventHubCompatibleEndpoint { get; set; }

        [Option(
            'n',
            "EventHubName",
            HelpText = "The event hub-compatible name of your IoT hub instance. Use `az iot hub show --query properties.eventHubEndpoints.events.path --name {your IoT hub name}` to fetch via the Azure CLI.")]
        public string EventHubName { get; set; }

        [Option(
            's',
            "SharedAccessKey",
            HelpText = "A primary or shared access key from your IoT hub instance, with the 'service' permission. Use `az iot hub policy show --name service --query primaryKey --hub-name {your IoT hub name}` to fetch via the Azure CLI.")]
        public string SharedAccessKey { get; set; }

        [Option(
            'c',
            "EventHubConnectionString",
            HelpText = "The connection string to the event hub-compatible endpoint. Use the Azure portal to get this parameter. If this value is provided, all the others are not necessary.")]
        public string EventHubConnectionString { get; set; }

        internal string GetEventHubConnectionString()
        {
            const string iotHubSharedAccessKeyName = "service";
            return EventHubConnectionString ?? $"Endpoint={EventHubCompatibleEndpoint};SharedAccessKeyName={iotHubSharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
        }
    }
}
