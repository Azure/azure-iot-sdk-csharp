﻿using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace EnvironmentalSensorSample
{
    /// <summary>
    /// A Special Digital Twin interface that provides the model definition when asked by the getModelDefinition command.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.IoT.DigitalTwin.Device.DigitalTwinInterfaceClient" />
    public class ModelDefinitionInterface : DigitalTwinInterfaceClient
    {
        private const string ModelDefinitionInterfaceId = "urn:azureiot:ModelDiscovery:ModelDefinition:1";

        private string environmentalSensorModelDefinition = null;

        private const string getModelDefinitionCommandName = "getModelDefinition";

        private const string EnvironmentalSensorInterfaceFileName = "EnvironmentalSensor.interface.json";
        private const string EnvironmentalSensorInterfaceFileAssemblyName = "EnvironmentalSensorSample";

        public ModelDefinitionInterface(string interfaceInstanceName)
            : base(ModelDefinitionInterfaceId, interfaceInstanceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyResourceName = EnvironmentalSensorInterfaceFileAssemblyName + "." + EnvironmentalSensorInterfaceFileName;

            using (Stream stream = assembly.GetManifestResourceStream(assemblyResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                this.environmentalSensorModelDefinition = reader.ReadToEnd();
            }
        }

                              /// <summary>
                              /// Callback on command received.
                              /// </summary>
                              /// <param name="commandRequest">information regarding the command received.</param>
                              /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            // There is only one command that ModelDefinition defines, and it is getModelDefinition. That command must specify the 
            // model Id in the payload, and the device must return the model definition in the command response payload
            if (getModelDefinitionCommandName.Equals(commandRequest.Name))
            {
                string commandPayload = JsonConvert.DeserializeObject<string>(commandRequest.Payload);
                if (commandPayload.Equals(EnvironmentalSensorInterface.EnvironmentalSensorInterfaceId))
                {
                    return Task<DigitalTwinCommandResponse>.Factory.StartNew(() => new DigitalTwinCommandResponse(StatusCodeCompleted, this.environmentalSensorModelDefinition));
                }
            }

            return Task<DigitalTwinCommandResponse>.Factory.StartNew(() => new DigitalTwinCommandResponse(StatusCodeNotImplemented, null));
        }
    }
}
